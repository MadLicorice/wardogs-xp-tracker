using System.Globalization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Media;

namespace WardogsXpTracker.App;

public sealed record XpSourceChartRow(string Label, decimal Earned, decimal Bonus, decimal Indeterminate,
    bool HasRecords, string Description);

public sealed record XpMissedChartRow(string Label, decimal Average, decimal Minimum, decimal Maximum,
    bool IsLowerBound, bool HasRecords, string Description);

internal static class AnalyticsPalette
{
    public static readonly Brush Earned = Frozen("#76D8AE");
    public static readonly Brush Bonus = Frozen("#E5C66A");
    public static readonly Brush Indeterminate = Frozen("#8995A7");
    public static readonly Brush Missed = Frozen("#DF9A70");
    public static readonly Brush Track = Frozen("#11171E");
    public static readonly Brush Text = Frozen("#E4EAF0");
    public static readonly Brush Muted = Frozen("#A8B4C2");

    private static Brush Frozen(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }

    public static string Amount(decimal value) => value.ToString(value == decimal.Truncate(value) ? "N0" : "N2", CultureInfo.CurrentCulture);
    public static string Exact(decimal value) => value.ToString("#,0.############################", CultureInfo.CurrentCulture);

    public static void TextAt(FrameworkElement owner, DrawingContext drawing, string text, Point at,
        Brush brush, double size = 11, double? maximumWidth = null)
    {
        var formatted = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), size, brush, VisualTreeHelper.GetDpi(owner).PixelsPerDip);
        if (maximumWidth is > 0)
        {
            formatted.MaxTextWidth = maximumWidth.Value;
            formatted.MaxLineCount = 1;
            formatted.Trimming = TextTrimming.CharacterEllipsis;
        }
        drawing.DrawText(formatted, at);
    }
}

/// <summary>Draws received XP on one shared scale. The view also supplies an exact-value text table.</summary>
public sealed class XpSourceBarChart : FrameworkElement
{
    private IReadOnlyList<XpSourceChartRow> rows = [];
    private const double RowHeight = 37;

    public XpSourceBarChart()
    {
        MinHeight = 222;
        MouseMove += (_, e) =>
        {
            var index = (int)(e.GetPosition(this).Y / RowHeight);
            ToolTip = index >= 0 && index < rows.Count ? rows[index].Description : null;
        };
    }

    public void SetRows(IReadOnlyList<XpSourceChartRow> value)
    {
        rows = value;
        System.Windows.Automation.AutomationProperties.SetName(this, "Received XP by class. Exact values appear in the table below.");
        InvalidateMeasure();
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize) => new(
        double.IsInfinity(availableSize.Width) ? 420 : Math.Max(0, availableSize.Width), Math.Max(1, rows.Count) * RowHeight);

    protected override void OnRender(DrawingContext drawing)
    {
        base.OnRender(drawing);
        drawing.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
        var left = 69d;
        var width = Math.Max(0, ActualWidth - left - 91);
        var maximum = Math.Max(1d, rows.Select(r => (double)(r.Earned + r.Bonus + r.Indeterminate)).DefaultIfEmpty(0).Max());
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var y = i * RowHeight;
            AnalyticsPalette.TextAt(this, drawing, row.Label, new(0, y + 7), AnalyticsPalette.Text, 11, left - 7);
            drawing.DrawRoundedRectangle(AnalyticsPalette.Track, null, new(left, y + 8, width, 14), 3, 3);
            var x = left;
            foreach (var segment in new[] { (row.Earned, AnalyticsPalette.Earned), (row.Bonus, AnalyticsPalette.Bonus), (row.Indeterminate, AnalyticsPalette.Indeterminate) })
            {
                var segmentWidth = Math.Clamp((double)segment.Item1 / maximum * width, 0, width);
                if (segmentWidth > 0) drawing.DrawRectangle(segment.Item2, null, new Rect(x, y + 8, segmentWidth, 14));
                x += segmentWidth;
            }
            var total = row.Earned + row.Bonus + row.Indeterminate;
            AnalyticsPalette.TextAt(this, drawing, row.HasRecords ? AnalyticsPalette.Amount(total) : "No records",
                new(left + width + 9, y + 7), row.HasRecords ? AnalyticsPalette.Text : AnalyticsPalette.Muted, 10, 82);
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new FrameworkElementAutomationPeer(this);
}

/// <summary>Draws only the same received-XP amounts used by the class bars.</summary>
public sealed class XpSourceDonut : FrameworkElement
{
    private decimal earned, bonus, indeterminate;

    public XpSourceDonut()
    {
        MinHeight = 210;
        ToolTip = "The donut uses the same recorded XP as the class bars. Missed bonuses are excluded.";
    }

    public void SetValues(decimal earnedXp, decimal bonusXp, decimal indeterminateXp)
    {
        earned = earnedXp; bonus = bonusXp; indeterminate = indeterminateXp;
        System.Windows.Automation.AutomationProperties.SetName(this,
            $"Recorded XP sources: in-match {AnalyticsPalette.Exact(earned)}, bonus {AnalyticsPalette.Exact(bonus)}, indeterminate {AnalyticsPalette.Exact(indeterminate)}.");
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize) => new(
        double.IsInfinity(availableSize.Width) ? 220 : Math.Max(0, availableSize.Width), 210);

    protected override void OnRender(DrawingContext drawing)
    {
        base.OnRender(drawing);
        var total = earned + bonus + indeterminate;
        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        var radius = Math.Max(0, Math.Min(ActualWidth, ActualHeight) * .39);
        if (radius <= 0) return;
        var thickness = radius * .30;
        drawing.DrawEllipse(null, new Pen(AnalyticsPalette.Track, thickness), center, radius, radius);
        var angle = -90d;
        if (total > 0)
        {
            foreach (var segment in new[] { (earned, AnalyticsPalette.Earned), (bonus, AnalyticsPalette.Bonus), (indeterminate, AnalyticsPalette.Indeterminate) })
            {
                if (segment.Item1 <= 0) continue;
                var fraction = (double)(segment.Item1 / total);
                if (fraction >= 1)
                    drawing.DrawEllipse(null, new Pen(segment.Item2, thickness), center, radius, radius);
                else
                {
                    var endAngle = angle + fraction * 360;
                    var geometry = new StreamGeometry();
                    using (var context = geometry.Open())
                    {
                        context.BeginFigure(At(center, radius, angle), false, false);
                        context.ArcTo(At(center, radius, endAngle), new Size(radius, radius), 0, fraction > .5,
                            SweepDirection.Clockwise, true, false);
                    }
                    geometry.Freeze();
                    drawing.DrawGeometry(null, new Pen(segment.Item2, thickness), geometry);
                }
                angle += fraction * 360;
            }
        }
        var number = AnalyticsPalette.Amount(total);
        var fontSize = number.Length > 10 ? 14 : number.Length > 7 ? 18 : 23;
        var numberText = new FormattedText(number, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI Semibold"), fontSize, AnalyticsPalette.Text, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        drawing.DrawText(numberText, new(center.X - numberText.Width / 2, center.Y - 20));
        var caption = total > 0 ? "XP recorded" : "No received XP";
        var captionText = new FormattedText(caption, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), 10, AnalyticsPalette.Muted, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        drawing.DrawText(captionText, new(center.X - captionText.Width / 2, center.Y + 10));
    }

    private static Point At(Point center, double radius, double angle)
    {
        var radians = angle * Math.PI / 180;
        return new(center.X + radius * Math.Cos(radians), center.Y + radius * Math.Sin(radians));
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new FrameworkElementAutomationPeer(this);
}

/// <summary>Draws average missed bonuses separately from received XP.</summary>
public sealed class XpMissedBarChart : FrameworkElement
{
    private IReadOnlyList<XpMissedChartRow> rows = [];
    private const double RowHeight = 33;

    public XpMissedBarChart()
    {
        MinHeight = 198;
        MouseMove += (_, e) =>
        {
            var index = (int)(e.GetPosition(this).Y / RowHeight);
            ToolTip = index >= 0 && index < rows.Count ? rows[index].Description : null;
        };
    }

    public void SetRows(IReadOnlyList<XpMissedChartRow> value)
    {
        rows = value;
        System.Windows.Automation.AutomationProperties.SetName(this, "Average missed bonus by class. These estimates do not enter received XP totals.");
        InvalidateMeasure();
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize) => new(
        double.IsInfinity(availableSize.Width) ? 420 : Math.Max(0, availableSize.Width), Math.Max(1, rows.Count) * RowHeight);

    protected override void OnRender(DrawingContext drawing)
    {
        base.OnRender(drawing);
        drawing.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
        var left = 69d;
        var width = Math.Max(0, ActualWidth - left - 116);
        var maximum = Math.Max(1d, rows.Select(r => (double)r.Average).DefaultIfEmpty(0).Max());
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var y = i * RowHeight;
            AnalyticsPalette.TextAt(this, drawing, row.Label, new(0, y + 6), AnalyticsPalette.Text, 11, left - 7);
            drawing.DrawRoundedRectangle(AnalyticsPalette.Track, null, new(left, y + 8, width, 12), 3, 3);
            var filled = Math.Clamp((double)row.Average / maximum * width, 0, width);
            if (filled > 0) drawing.DrawRoundedRectangle(AnalyticsPalette.Missed, null, new(left, y + 8, filled, 12), 3, 3);
            var text = row.HasRecords ? (row.IsLowerBound ? "At least " : "") + AnalyticsPalette.Amount(row.Average) : "No records";
            AnalyticsPalette.TextAt(this, drawing, text, new(left + width + 9, y + 6), AnalyticsPalette.Muted, 10, 107);
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new FrameworkElementAutomationPeer(this);
}
