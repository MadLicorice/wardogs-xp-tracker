using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using WardogsXpTracker.Core.Analytics;

namespace WardogsXpTracker.App;

/// <summary>Displays a calculated local report. It never changes saved sessions or baselines.</summary>
public partial class AnalyticsView : UserControl
{
    public SeasonAnalytics? Report { get; private set; }
    public bool IsDemo { get; private set; }

    public AnalyticsView()
    {
        InitializeComponent();
        ScopeText.Text = "Select a saved season to view recorded XP.";
        EmptyStatePanel.Visibility = Visibility.Visible;
    }

    public void SetReport(SeasonAnalytics report, bool isDemo = false)
    {
        ArgumentNullException.ThrowIfNull(report);
        Dispatcher.VerifyAccess();
        Report = report;
        IsDemo = isDemo;
        DemoBanner.Visibility = isDemo ? Visibility.Visible : Visibility.Collapsed;
        ReportTitle.Text = isDemo ? $"{report.SeasonName} · Demo" : report.SeasonName;
        var coverage = report.Coverage;
        var tracked = report.Records.Where(r => r.Status != AnalyticsRecordStatus.Disabled).ToArray();
        var outside = tracked.Where(r => !r.IncludedInReceived).ToArray();
        var unresolved = tracked.Where(r => r.IncludedInReceived
            && (r.Status == AnalyticsRecordStatus.RulesMismatch || r.Split?.IndeterminateXp > 0)).ToArray();
        var included = tracked.Count(r => r.IncludedInReceived);
        var ruleMismatches = unresolved.Count(r => r.Status == AnalyticsRecordStatus.RulesMismatch);
        ScopeText.Text = $"{Count(coverage.Sessions, "match", "matches")} · {Count(tracked.Length, "tracked class result", "tracked class results")} · {included:N0} included in received XP";
        var outsideCounts = outside.GroupBy(r => r.Status).OrderBy(g => g.Key)
            .Select(g => $"{g.Count():N0} {StatusText(g.Key).ToLower(CultureInfo.CurrentCulture)}");
        CoverageText.Text = outside.Length == 0 ? "No tracked class results are pending or incomplete."
            : "Outside received totals: " + string.Join(" · ", outsideCounts) + ".";
        if (unresolved.Length > 0)
            CoverageText.Text += $"\nIncluded with indeterminate sources: {Count(unresolved.Length, "class result", "class results")} · "
                + Count(ruleMismatches, "rules mismatch", "rules mismatches") + ". Their known received XP stays in the totals.";
        DisabledSlotsText.Text = $"{report.Records.Count(r => r.Status == AnalyticsRecordStatus.Disabled):N0} role slots were not tracked. "
            + "These slots are omitted from the pending list and the tracked-result count.";

        var totals = report.Totals;
        var hasReceivedRecords = coverage.IncludedRecords > 0;
        SetAmount(ReceivedTotalText, totals.TotalXp, hasReceivedRecords);
        SetAmount(EarnedTotalText, totals.EarnedXp, hasReceivedRecords);
        SetAmount(BonusTotalText, totals.BonusXp, hasReceivedRecords);
        SetAmount(IndeterminateTotalText, totals.IndeterminateXp, hasReceivedRecords);
        EarnedShareText.Text = Share(totals.EarnedXp, totals.TotalXp);
        BonusShareText.Text = Share(totals.BonusXp, totals.TotalXp);
        IndeterminateShareText.Text = Share(totals.IndeterminateXp, totals.TotalXp);
        EarnedShareText.ToolTip = AnalyticsPalette.Exact(totals.EarnedXp) + " XP";
        BonusShareText.ToolTip = AnalyticsPalette.Exact(totals.BonusXp) + " XP";
        IndeterminateShareText.ToolTip = AnalyticsPalette.Exact(totals.IndeterminateXp) + " XP";
        SourceDonut.SetValues(totals.EarnedXp, totals.BonusXp, totals.IndeterminateXp);

        var classes = report.Classes.OrderBy(c => c.Class).ToArray();
        var rows = classes.Select(ToClassRow).ToArray();
        SetTableRows(SourceTable, rows, nameof(ClassRow.ClassOrder), ListSortDirection.Ascending);
        SetTableRows(MissedTable, rows.ToArray(), nameof(ClassRow.ClassOrder), ListSortDirection.Ascending);
        SourceBars.SetRows(classes.Select(c => new XpSourceChartRow(c.Class.ToString(), c.Received.EarnedXp,
            c.Received.BonusXp, c.Received.IndeterminateXp, c.IncludedRecords > 0, SourceDescription(c))).ToArray());
        MissedBars.SetRows(classes.Select(c => new XpMissedChartRow(c.Class.ToString(), c.AverageMissedBonus,
            c.MinimumMissedBonus, c.MaximumMissedBonus, c.HasLowerBoundMissedBonus, c.MissedRecords > 0, MissedDescription(c))).ToArray());

        EmptyStatePanel.Visibility = !hasReceivedRecords || totals.TotalXp == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyStateText.Text = !hasReceivedRecords
            ? "No records qualify for received-XP totals yet. Confirm starting coverage and final results where available. Pending and incomplete records remain separate below."
            : totals.TotalXp == 0 ? "The included records contain zero received XP. The source percentages are unavailable because the recorded total is zero." : "";

        var missedRecords = classes.Sum(c => c.MissedRecords);
        MissedTotalText.Text = missedRecords == 0 ? "—" : (report.HasLowerBoundMissedBonus ? "At least " : "") + AnalyticsPalette.Amount(report.AverageMissedBonus) + " XP";
        MissedTotalText.ToolTip = missedRecords == 0 ? "No confirmed no-bonus records are available."
            : AnalyticsPalette.Exact(report.AverageMissedBonus) + " XP · " + Count(missedRecords, "class result", "class results");
        MissedRangeText.Text = missedRecords == 0 ? "No confirmed no-bonus records" : Range(report.MinimumMissedBonus,
            report.MaximumMissedBonus, report.HasLowerBoundMissedBonus);

        var uncertain = unresolved.Select(ToRecordRow).ToArray();
        SetTableRows(UncertaintyRecords, uncertain, nameof(RecordRow.StartedAt), ListSortDirection.Descending);
        UncertaintyRecords.Visibility = uncertain.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        UncertaintySummary.Text = uncertain.Length == 0 ? "No included class result has an indeterminate source amount."
            : $"{Count(uncertain.Length, "included class result contains", "included class results contain")} indeterminate XP. Their known totals remain in the charts. Each explanation states the missing evidence or rule mismatch.";
        var excluded = outside.Select(ToRecordRow).ToArray();
        SetTableRows(ExcludedRecords, excluded, nameof(RecordRow.StartedAt), ListSortDirection.Descending);
        ExcludedRecords.Visibility = excluded.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        ExcludedHeading.Text = excluded.Length == 0 ? "No pending or incomplete tracked class results"
            : $"Pending and incomplete class results · {excluded.Length:N0}";
        MethodText.Text = $"Calculated using each match's saved bonus rules · Method {report.DerivationVersion} · Generated {report.GeneratedAt.ToLocalTime():g}. "
            + "Source amounts use the common minimums across supported splits. The gray remainder is not assigned to a guessed placement. "
            + "Further analytics are planned for a future season. You can also further analyze your local save files.";
    }

    private static string Count(int value, string singular, string plural) => $"{value:N0} {(value == 1 ? singular : plural)}";

    private static void SetTableRows<T>(DataGrid table, T[] rows, string defaultProperty, ListSortDirection defaultDirection)
    {
        var sorts = table.Items.SortDescriptions.Where(s => typeof(T).GetProperty(s.PropertyName) is not null).ToArray();
        if (sorts.Length == 0) sorts = [new SortDescription(defaultProperty, defaultDirection)];
        table.ItemsSource = rows;
        using (table.Items.DeferRefresh())
        {
            table.Items.SortDescriptions.Clear();
            foreach (var sort in sorts) table.Items.SortDescriptions.Add(sort);
        }
        foreach (var column in table.Columns)
        {
            var sort = sorts.FirstOrDefault(s => s.PropertyName == column.SortMemberPath);
            column.SortDirection = !string.IsNullOrEmpty(sort.PropertyName) ? sort.Direction : null;
        }
    }

    private static void SetAmount(TextBlock target, decimal amount, bool hasRecords)
    {
        target.Text = hasRecords ? AnalyticsPalette.Amount(amount) : "—";
        target.ToolTip = hasRecords ? AnalyticsPalette.Exact(amount) + " XP" : "No included records. Unknown values are not zero.";
    }

    private static string Share(decimal amount, decimal total) => total <= 0 ? "—"
        : (amount / total).ToString("P1", CultureInfo.CurrentCulture);

    private static ClassRow ToClassRow(ClassAnalytics item)
    {
        var present = item.IncludedRecords > 0;
        return new(item.Class.ToString(), present ? AnalyticsPalette.Exact(item.Received.EarnedXp) : "—",
            present ? AnalyticsPalette.Exact(item.Received.BonusXp) : "—", present ? AnalyticsPalette.Exact(item.Received.IndeterminateXp) : "—",
            present ? AnalyticsPalette.Exact(item.Received.TotalXp) : "—", item.IncludedRecords.ToString("N0", CultureInfo.CurrentCulture),
            item.MissedRecords == 0 ? "—" : (item.HasLowerBoundMissedBonus ? "At least " : "") + AnalyticsPalette.Exact(item.AverageMissedBonus) + " XP",
            item.MissedRecords == 0 ? "No confirmed no-bonus records" : Range(item.MinimumMissedBonus, item.MaximumMissedBonus, item.HasLowerBoundMissedBonus),
            SourceDescription(item), MissedDescription(item))
        {
            ClassOrder = (int)item.Class,
            EarnedXp = present ? item.Received.EarnedXp : null,
            BonusXp = present ? item.Received.BonusXp : null,
            IndeterminateXp = present ? item.Received.IndeterminateXp : null,
            TotalXp = present ? item.Received.TotalXp : null,
            IncludedCount = item.IncludedRecords,
            AverageMissedXp = item.MissedRecords > 0 ? item.AverageMissedBonus : null,
            MinimumMissedXp = item.MissedRecords > 0 ? item.MinimumMissedBonus : null,
            MaximumMissedXp = item.MissedRecords > 0 ? item.MaximumMissedBonus : null,
            MinimumMissedText = item.MissedRecords > 0 ? AnalyticsPalette.Exact(item.MinimumMissedBonus) : "—",
            MaximumMissedText = item.MissedRecords > 0 ? AnalyticsPalette.Exact(item.MaximumMissedBonus) : "—"
        };
    }

    private static string SourceDescription(ClassAnalytics item) => item.IncludedRecords == 0
        ? $"{item.Class}: no included records. Missing totals are not zero."
        : $"{item.Class}: {AnalyticsPalette.Exact(item.Received.EarnedXp)} in-match XP, {AnalyticsPalette.Exact(item.Received.BonusXp)} bonus XP, "
            + $"{AnalyticsPalette.Exact(item.Received.IndeterminateXp)} indeterminate XP. Total {AnalyticsPalette.Exact(item.Received.TotalXp)} XP across {Count(item.IncludedRecords, "class result", "class results")}.";

    private static string MissedDescription(ClassAnalytics item) => item.MissedRecords == 0
        ? $"{item.Class}: no confirmed no-bonus records. Unknown outcomes do not count as missed bonuses."
        : $"{item.Class}: {(item.HasLowerBoundMissedBonus ? "at least " : "")}{AnalyticsPalette.Exact(item.AverageMissedBonus)} XP average missed bonus. "
            + Range(item.MinimumMissedBonus, item.MaximumMissedBonus, item.HasLowerBoundMissedBonus)
            + $". {Count(item.MissedRecords, "class result", "class results")}. This estimate stays outside received XP.";

    private static string Range(decimal minimum, decimal maximum, bool lowerBound) =>
        $"{(lowerBound ? "Observed-tier range" : "Placement range")} {AnalyticsPalette.Exact(minimum)}–{AnalyticsPalette.Exact(maximum)} XP"
        + (lowerBound ? " · More XP may be unrecorded" : "");

    private static RecordRow ToRecordRow(SessionClassAnalytics item)
    {
        var explanation = item.Explanation;
        if (!string.IsNullOrWhiteSpace(item.Split?.Explanation) && !explanation.Contains(item.Split.Explanation, StringComparison.Ordinal))
            explanation += " " + item.Split.Explanation;
        if (!string.IsNullOrWhiteSpace(item.SourceNotes)) explanation += " " + item.SourceNotes;
        return new(item.StartedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture), item.Class.ToString(),
            item.TotalGain is { } gain ? AnalyticsPalette.Exact(gain) : "Unknown", StatusText(item.Status), explanation)
        {
            StartedAt = item.StartedAt,
            ClassOrder = (int)item.Class,
            Gain = item.TotalGain
        };
    }

    private static string StatusText(AnalyticsRecordStatus status) => status switch
    {
        AnalyticsRecordStatus.Included => "Included",
        AnalyticsRecordStatus.Disabled => "Tracking disabled",
        AnalyticsRecordStatus.MissingBaseline => "Baseline missing",
        AnalyticsRecordStatus.UnconfirmedBaseline => "Start unconfirmed",
        AnalyticsRecordStatus.PartialCoverage => "Partial coverage",
        AnalyticsRecordStatus.PendingFinal => "Final pending",
        AnalyticsRecordStatus.MissingFinal => "Ending total missing",
        AnalyticsRecordStatus.RulesMismatch => "Rules mismatch",
        AnalyticsRecordStatus.ConflictingEvidence => "Evidence conflicts",
        AnalyticsRecordStatus.InvalidTotal => "Invalid total",
        _ => status.ToString()
    };

    private sealed record ClassRow(string ClassName, string EarnedText, string BonusText, string IndeterminateText,
        string TotalText, string IncludedText, string MissedText, string MissedRangeText, string SourceDescription, string MissedDescription)
    {
        public int ClassOrder { get; init; }
        public decimal? EarnedXp { get; init; }
        public decimal? BonusXp { get; init; }
        public decimal? IndeterminateXp { get; init; }
        public decimal? TotalXp { get; init; }
        public int IncludedCount { get; init; }
        public decimal? AverageMissedXp { get; init; }
        public decimal? MinimumMissedXp { get; init; }
        public decimal? MaximumMissedXp { get; init; }
        public string MinimumMissedText { get; init; } = "—";
        public string MaximumMissedText { get; init; } = "—";
    }
    private sealed record RecordRow(string MatchText, string ClassName, string GainText, string StateText, string Explanation)
    {
        public DateTimeOffset StartedAt { get; init; }
        public int ClassOrder { get; init; }
        public long? Gain { get; init; }
    }
}
