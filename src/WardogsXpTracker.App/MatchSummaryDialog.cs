using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using WardogsXpTracker.Core.Analytics;
using WardogsXpTracker.Core.Reading;
using WardogsXpTracker.Core.Tracking;

namespace WardogsXpTracker.App;

public sealed record MatchSummaryClassRow
{
    public XpClass Class { get; init; }
    public long? StartingXp { get; init; }
    public long? FinalXp { get; init; }
    public long? TotalXp { get; init; }
    public long? ActionsXp { get; init; }
    public long? BonusXp { get; init; }
    public long? IndeterminateXp { get; init; }
    public bool HasSourceSplit { get; init; }
    public bool HasWholeMatchCoverage { get; init; }
    public string BaselineLabel { get; init; } = "Starting baseline";
    public string Explanation { get; init; } = "";
    public string ClassName => Class.ToString();
    public string StartingText => Amount(StartingXp);
    public string FinalText => Amount(FinalXp);
    public string Accent => HasSourceSplit && IndeterminateXp == 0 ? "#8DDDB5" : "#B7C2D1";
    public string FormulaText => TotalXp is not { } total ? "Total Unknown · Actions Unknown · Bonus Unknown"
        : HasSourceSplit && ActionsXp is { } actions && BonusXp is { } bonus
            ? $"Total {Amount(total)} = Actions {Amount(actions)} + Bonus {Amount(bonus)}"
                + (IndeterminateXp is > 0 ? $" + Indeterminate {Amount(IndeterminateXp)}" : "")
            : $"Total {Amount(total)} XP · Actions Unknown · Bonus Unknown\nIndeterminate {Amount(total)} XP";

    private static string Amount(long? value) => value?.ToString("N0", CultureInfo.CurrentCulture) ?? "Unknown";
}

public sealed record MatchSummary
{
    public Guid SessionId { get; init; }
    public string PlacementText { get; init; } = "Not observed";
    public string RulesName { get; init; } = "";
    public decimal? RecordedXp { get; init; }
    public int KnownClassCount { get; init; }
    public decimal? StartingCumulativeXp { get; init; }
    public int KnownStartingClassCount { get; init; }
    public decimal? FinalCumulativeXp { get; init; }
    public int KnownFinalClassCount { get; init; }
    public decimal? ActionsXp { get; init; }
    public decimal? BonusXp { get; init; }
    public decimal? IndeterminateXp { get; init; }
    public int SourceSplitClassCount { get; init; }
    public bool NeedsBaselineConfirmation { get; init; }
    public IReadOnlyList<MatchSummaryClassRow> Classes { get; init; } = [];
    public decimal? SupportedActionsXp => SumSupported(true);
    public decimal? SupportedBonusXp => SumSupported(false);
    public string Headline => RecordedXp is { } total ? $"{Amount(total)} XP recorded" : "Recorded XP unknown";
    public string ScopeText => $"{KnownClassCount} of 6 class increases known · {SourceSplitClassCount} source breakdown(s) supported";
    public string CumulativeText => $"Known starting cumulative XP: {Amount(StartingCumulativeXp)} ({KnownStartingClassCount}/6 classes)"
        + $"\nKnown final cumulative XP: {Amount(FinalCumulativeXp)} ({KnownFinalClassCount}/6 classes)";
    public string FormulaText => RecordedXp is { } total && ActionsXp is { } actions && BonusXp is { } bonus
        ? $"Total {Amount(total)} = Actions {Amount(actions)} + Bonus {Amount(bonus)}"
            + (IndeterminateXp is > 0 ? $" + Indeterminate {Amount(IndeterminateXp)}" : "")
        : RecordedXp is { } recorded && IndeterminateXp is { } unknown
            ? $"{Amount(recorded)} XP total · {Amount(unknown)} XP indeterminate source\nActions Unknown · Bonus Unknown"
            : "Total Unknown · Actions Unknown · Bonus Unknown";

    private static string Amount(decimal? value) => value?.ToString("N0", CultureInfo.CurrentCulture) ?? "Unknown";

    private decimal? SumSupported(bool actions)
    {
        var rows = Classes.Where(row => row.TotalXp is not null && row.HasSourceSplit
            && row.ActionsXp is not null && row.BonusXp is not null).ToArray();
        return rows.Length == 0 ? null : rows.Sum(row => actions ? (decimal)row.ActionsXp!.Value : row.BonusXp!.Value);
    }
}

/// <summary>Shows a read-only result after the caller saves final XP. It never changes session data.</summary>
public static class MatchSummaryDialog
{
    public static MatchSummary BuildSummary(TrackerSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var final = session.ConfirmedFinal;
        IReadOnlyList<SessionClassAnalytics> records = [];
        string? analysisError = null;
        try
        {
            records = AnalyticsEngine.Build(new SeasonDocument
            {
                SeasonId = session.Id, Name = "Match summary", ActiveSessionId = session.Id, Sessions = [session]
            }, final?.ConfirmedAt ?? session.EndedAt ?? session.StartedAt).Records;
        }
        catch (InvalidDataException exception)
        { analysisError = $"Source calculation unavailable: {exception.Message}"; }

        var rows = Enum.GetValues<XpClass>().Select(trackClass =>
        {
            session.Tracks.TryGetValue(trackClass, out var track);
            XpObservation? observedFinal = null;
            if (final is not null) final.Tracks.TryGetValue(trackClass, out observedFinal);
            var original = track?.OriginalBaseline;
            var wholeStart = original is { Validity: BaselineValidity.Valid, Coverage: BaselineCoverage.BeforeEarnedXp };
            var baseline = wholeStart ? original : track?.Baseline ?? original;
            var startingXp = baseline?.Xp;
            var finalXp = observedFinal?.Xp;
            long? total = startingXp is >= 0 and var start && finalXp is { } end && end >= start ? end - start : null;
            var record = records.FirstOrDefault(item => item.Class == trackClass);
            var split = record is { IncludedInReceived: true, Split.Status: XpSplitStatus.Exact or XpSplitStatus.Ambiguous }
                && record.StartingXp == startingXp && record.EndingXp == finalXp ? record.Split : null;
            var explanation = finalXp is null ? "Final cumulative XP is unknown. No final total was recorded for this class."
                : startingXp is null ? "Starting cumulative XP is unknown. A class increase cannot be calculated."
                : startingXp < 0 || finalXp < 0 ? "The starting or final XP is invalid. Review these values."
                : total is null ? "The final XP is below the displayed baseline. Review these values."
                : split is not null ? split.Explanation
                : baseline?.Coverage == BaselineCoverage.Partial || track?.WasRebased == true && !wholeStart
                    ? "Measured since a rebased baseline. Whole-match coverage and the source split are unavailable."
                : !wholeStart ? "Measured since an uncertified baseline. Source split unavailable. This does not establish the whole match."
                : analysisError ?? record?.Explanation ?? "Source split unavailable for this class.";
            if (split is not null && track?.WasRebased == true)
                explanation += " The valid original starting baseline supplies this whole-match calculation.";
            if (analysisError is not null && total is not null && !explanation.Contains(analysisError, StringComparison.Ordinal))
                explanation += " " + analysisError;
            return new MatchSummaryClassRow
            {
                Class = trackClass, StartingXp = startingXp, FinalXp = finalXp, TotalXp = total,
                ActionsXp = split?.MinimumEarnedXp, BonusXp = split?.MinimumBonusXp,
                IndeterminateXp = split?.IndeterminateXp ?? total,
                HasSourceSplit = split is not null, HasWholeMatchCoverage = wholeStart && total is not null,
                BaselineLabel = baseline is null ? "Baseline not recorded"
                    : baseline.Coverage == BaselineCoverage.Partial ? "Rebased baseline"
                    : wholeStart ? "Original starting baseline" : "Uncertified baseline",
                Explanation = explanation
            };
        }).ToArray();
        var known = rows.Where(row => row.TotalXp is not null).ToArray();
        var supported = known.Where(row => row.HasSourceSplit).ToArray();
        var allKnownSources = known.Length > 0 && supported.Length == known.Length;
        return new()
        {
            SessionId = session.Id, PlacementText = PlacementText(final), RulesName = session.RulesSnapshot?.Name ?? "Unknown rules",
            Classes = Array.AsReadOnly(rows), KnownClassCount = known.Length,
            RecordedXp = known.Length == 0 ? null : known.Sum(row => (decimal)row.TotalXp!.Value),
            KnownStartingClassCount = rows.Count(row => row.StartingXp is not null),
            StartingCumulativeXp = SumKnown(rows.Select(row => row.StartingXp)),
            KnownFinalClassCount = rows.Count(row => row.FinalXp is not null),
            FinalCumulativeXp = SumKnown(rows.Select(row => row.FinalXp)),
            SourceSplitClassCount = supported.Length,
            NeedsBaselineConfirmation = session.Tracks.Values.Any(track => track.Enabled
                && track.OriginalBaseline is not { Validity: BaselineValidity.Valid, Coverage: BaselineCoverage.BeforeEarnedXp }),
            ActionsXp = allKnownSources ? supported.Sum(row => (decimal)row.ActionsXp!.Value) : null,
            BonusXp = allKnownSources ? supported.Sum(row => (decimal)row.BonusXp!.Value) : null,
            IndeterminateXp = known.Length == 0 ? null : known.Sum(row => (decimal)row.IndeterminateXp!.Value)
        };
    }

    public static void Show(Window owner, TrackerSession session)
    {
        ArgumentNullException.ThrowIfNull(owner);
        owner.Dispatcher.VerifyAccess();
        var summary = BuildSummary(session);
        var panel = new StackPanel { Margin = new Thickness(24, 20, 24, 18) };
        panel.Children.Add(Text("MATCH XP RECORDED!", 18, "#76F0AA", FontWeights.Bold));
        panel.Children.Add(Text(summary.RecordedXp is { } total ? $"{Amount(total)} XP" : "Unknown XP",
            42, summary.RecordedXp is null ? "#A9B4C5" : "#F1F5F8", FontWeights.Bold, new(0, 4, 0, 14)));
        var parts = new System.Windows.Controls.Primitives.UniformGrid { Columns = 3 };
        parts.Children.Add(Part("Actions", summary.SupportedActionsXp, "#76D8AE"));
        parts.Children.Add(Part("Bonus", summary.SupportedBonusXp, "#E5C66A"));
        parts.Children.Add(Part("Indeterminate", summary.IndeterminateXp, "#A9B4C5"));
        panel.Children.Add(parts);
        var knownRows = summary.Classes.Where(row => row.TotalXp is not null).ToArray();
        if (summary.NeedsBaselineConfirmation)
        {
            var warning = Text("For a proper Actions XP vs Bonus XP breakdown, confirm starting baselines in For Analytics ONLY. "
                + "For earlier matches, use Saved Results → Starting baselines.",
                12, "#F1CF69", FontWeights.SemiBold, new(0, 12, 0, 0));
            warning.TextDecorations = TextDecorations.Underline;
            warning.Tag = "BaselineConfirmationWarning";
            panel.Children.Add(warning);
        }
        if (knownRows.Length > 0)
        {
            var classes = new StackPanel { Margin = new Thickness(0, 14, 0, 0), Tag = "RecordedClassRows" };
            foreach (var row in knownRows) classes.Children.Add(CompactRow(row));
            panel.Children.Add(classes);
        }

        var workArea = SystemParameters.WorkArea;
        var maximumWidth = Math.Min(650, Math.Max(1, workArea.Width - 24));
        var maximumHeight = Math.Min(520, Math.Max(1, workArea.Height - 24));
        var dialog = new Window
        {
            Owner = owner, Title = "Match XP summary", WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Width = Math.Min(620, maximumWidth), MaxWidth = maximumWidth, SizeToContent = SizeToContent.Height,
            MaxHeight = maximumHeight,
            Background = Brush("#11151B"), Foreground = Brush("#E8EDF2"), FontFamily = new("Segoe UI"),
            ShowInTaskbar = false, ResizeMode = ResizeMode.NoResize
        };
        var layout = new DockPanel();
        var footer = new Border { Padding = new Thickness(24, 10, 24, 14), Background = Brush("#19212B") };
        var close = new Button { Content = "Continue", IsDefault = true, IsCancel = true,
            HorizontalAlignment = HorizontalAlignment.Right, MinWidth = 120, Padding = new Thickness(18, 8, 18, 8) };
        close.Click += (_, _) => dialog.Close();
        footer.Child = close;
        DockPanel.SetDock(footer, Dock.Bottom);
        layout.Children.Add(footer);
        layout.Children.Add(new ScrollViewer
        {
            Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, CanContentScroll = false
        });
        dialog.Content = layout;
        dialog.ShowDialog();
    }

    private static decimal? SumKnown(IEnumerable<long?> values)
    {
        var known = values.Where(value => value is not null).ToArray();
        return known.Length == 0 ? null : known.Sum(value => (decimal)value!.Value);
    }

    private static string PlacementText(FinalResultSet? final)
    {
        if (final is null) return "Not observed";
        if (final.HasConflict) return "Needs review · Placement evidence conflicts";
        var places = final.PossiblePlaces.Where(place => place is >= 1 and <= 3).Distinct().Order().ToArray();
        if (places.Length == 0)
        {
            var evidence = final.PlacementEvidence.Where(item => item.PossiblePlaces.Count > 0).ToArray();
            if (evidence.Length == 0) return "Not observed";
            places = evidence.Select(item => item.PossiblePlaces.AsEnumerable()).Aggregate((a, b) => a.Intersect(b)).Distinct().Order().ToArray();
            if (places.Length == 0) return "Needs review · Placement evidence conflicts";
        }
        var ordinal = places.Select(place => place switch { 1 => "1st", 2 => "2nd", _ => "3rd" }).ToArray();
        var text = ordinal.Length == 1 ? ordinal[0] : string.Join(" or ", ordinal);
        var hasDefeat = final.PlacementEvidence.Any(item => item.RawText.Contains("DEFEAT", StringComparison.OrdinalIgnoreCase));
        var hasVictory = final.PlacementEvidence.Any(item => item.RawText.Contains("VICTORY", StringComparison.OrdinalIgnoreCase));
        return text + " place" + (hasDefeat ? " · DEFEAT" : hasVictory ? " · VICTORY" : "");
    }

    private static StackPanel Part(string label, decimal? value, string color)
    {
        var part = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
        var tint = value is null ? "#A9B4C5" : color;
        part.Children.Add(Text(label, 11, tint));
        part.Children.Add(Text(Amount(value), 24, tint, FontWeights.SemiBold, new(0, 3, 0, 0)));
        return part;
    }

    private static TextBlock CompactRow(MatchSummaryClassRow row)
    {
        var line = Text("", 12, "#D7E1EA", margin: new(0, 5, 0, 5));
        line.Tag = row.Class;
        line.Inlines.Add(new Run($"{row.ClassName} · {Amount(row.TotalXp)} XP  ") { FontWeight = FontWeights.SemiBold });
        Add("Actions", row.ActionsXp, "#76D8AE");
        line.Inlines.Add(new Run(" + ") { Foreground = Brush("#738497") });
        Add("Bonus", row.BonusXp, "#E5C66A");
        line.Inlines.Add(new Run(" + ") { Foreground = Brush("#738497") });
        Add("Indeterminate", row.IndeterminateXp, "#A9B4C5");
        return line;

        void Add(string label, long? value, string color)
            => line.Inlines.Add(new Run($"{label} {Amount(value)}") { Foreground = Brush(value is null ? "#A9B4C5" : color) });
    }

    private static string Amount(decimal? value) => value?.ToString("N0", CultureInfo.CurrentCulture) ?? "Unknown";
    private static TextBlock Text(string text, double size, string color, FontWeight? weight = null, Thickness? margin = null)
        => new()
        {
            Text = text, FontSize = size, Foreground = Brush(color), FontWeight = weight ?? FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap, Margin = margin ?? new Thickness(0)
        };

    private static Brush Brush(string color) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
}
