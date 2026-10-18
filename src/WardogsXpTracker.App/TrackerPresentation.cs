using System.Windows;
using System.Windows.Media;
using WardogsXpTracker.Core.Reading;
using WardogsXpTracker.Core.Tracking;

namespace WardogsXpTracker.App;

public sealed record CoverageCard(XpClass Class, string ClassName, string StatusText,
    string ActionText, bool CanConfirm, string Accent);

public partial class MainWindow
{
    private static bool StartingBaselineConfirmed(TrackState track) =>
        track.OriginalBaseline is { Validity: BaselineValidity.Valid, Coverage: BaselineCoverage.BeforeEarnedXp };

    private static bool CanConfirmStartingBaseline(TrackState track) =>
        track.Enabled && !track.WasRebased && track.Baseline is { } baseline
        && track.OriginalBaseline is { } original && baseline.Id == original.Id && baseline.Version == original.Version
        && baseline.Validity != BaselineValidity.Invalid && baseline.Coverage != BaselineCoverage.Partial
        && original.Validity != BaselineValidity.Invalid && original.Coverage != BaselineCoverage.Partial
        && !StartingBaselineConfirmed(track);

    private void RefreshSessionPresentation()
    {
        var session = tracker?.ActiveSession;
        var inactive = !trackerReady || session is null || resumePending;
        InactiveSessionOverlay.Visibility = inactive ? Visibility.Visible : Visibility.Collapsed;
        InactiveSessionTitle.Text = !trackerReady ? persistenceBlocked ? "Tracking unavailable" : "Loading local save…" : "No Active Session";
        InactiveSessionMessage.Text = !trackerReady
            ? persistenceBlocked ? "The local save could not load. Read the save status, then restart after correcting the error." : "Your saved seasons and preferences will appear shortly."
            : resumePending ? "Resume the saved session for the same match, or start a new session. Baseline snapshots are paused until you choose."
            : "Select New Session before recording baselines. Then show each class's current UNLOCK page and use the snapshot key.";
        StartSessionOverlayButton.Visibility = trackerReady ? Visibility.Visible : Visibility.Collapsed;
        ResumeOverlayButton.Visibility = resumePending ? Visibility.Visible : Visibility.Collapsed;
        SessionHeading.Foreground = Brush(!inactive && session?.State == TrackerSessionState.Active ? "#7AE3A7" : "#F1CF69");

        var enabled = session?.Tracks.Values.Where(t => t.Enabled).ToArray() ?? [];
        var confirmed = enabled.Count(StartingBaselineConfirmed);
        var allConfirmed = enabled.Length > 0 && confirmed == enabled.Length;
        AnalyticsPanel.BorderBrush = Brush(allConfirmed ? "#65C995" : enabled.Length == 0 && session is not null ? "#667180" : "#D06E78");
        AnalyticsPanel.Background = Brush(allConfirmed ? "#172C25" : "#251E25");
        AnalyticsSummary.Text = session is null ? "Start a session, then confirm starting XP for accurate analytics. Tracking works without this confirmation."
            : enabled.Length == 0 ? "No classes selected. Enable a class to include its starting XP."
            : allConfirmed ? session?.ConfirmedFinal is not null
                ? "Starting baselines confirmed. Final results recorded. See Analytics for the supported breakdown."
                : "Starting baselines confirmed. Final results are still required for complete match analytics."
            : $"Confirm starting XP for accurate analytics. {confirmed} of {enabled.Length} tracked classes confirmed. The XP target works without confirmation.";
        AnalyticsBonusStatus.Text = session?.PendingFinal is not null ? "Final results observed · Awaiting your confirmation. Bonus receipt is separate from the XP target."
            : session?.ConfirmedFinal is not null ? "Final results confirmed. Unknown bonus attribution stays indeterminate."
            : session?.Outcome is SessionOutcome.LeftMatch or SessionOutcome.Disconnected ? "No bonus reported: you marked this match as left or disconnected."
            : "Bonus receipt is separate. Confirm received bonuses through final results at the end of each match.";

        CoverageCards.ItemsSource = Enum.GetValues<XpClass>().Select(trackClass =>
        {
            var track = session?.Tracks[trackClass];
            var isEnabled = track?.Enabled ?? tracker?.Document.EnabledTracks.GetValueOrDefault(trackClass, true) ?? true;
            var isConfirmed = track is not null && StartingBaselineConfirmed(track);
            var canConfirm = track is not null && CanConfirmStartingBaseline(track);
            var status = !isEnabled ? "Tracking disabled · Not required"
                : track?.Baseline is null ? "Starting baseline not recorded"
                : isConfirmed ? "Starting XP confirmed for analytics"
                : track.WasRebased ? "Partial interval · Original start unconfirmed"
                : "Confirm starting XP for accurate analytics";
            return new CoverageCard(trackClass, trackClass.ToString(), status,
                isConfirmed ? "Confirmed" : "Confirm…", canConfirm,
                !isEnabled ? "#8D98A7" : isConfirmed ? "#86DCAF" : "#F0A0A6");
        }).ToArray();
    }

    private static SolidColorBrush Brush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }

    private async void ConfirmTrackCoverage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: XpClass trackClass })
            await ConfirmAnalyticsBaselinesAsync(trackClass);
    }

    private async Task ConfirmAnalyticsBaselinesAsync(XpClass? onlyClass)
    {
        if (!CanChangeTracker() || resumePending || tracker?.ActiveSession is not { } session) return;
        var eligible = session.Tracks.Values.Where(CanConfirmStartingBaseline)
            .Where(t => onlyClass is null || t.Class == onlyClass).ToArray();
        if (eligible.Length == 0)
        { SessionNotice.Text = "No unconfirmed starting baseline is available for this action. Capture an original baseline before confirming its timing."; return; }

        var owner = tracker;
        var versions = eligible.ToDictionary(t => t.Class, t => (t.Baseline!.Id, t.Baseline.Version));
        var list = string.Join(Environment.NewLine, eligible.Select(t => $"{t.Class}: {t.Baseline!.Xp:N0} XP"));
        var message = $"{list}{Environment.NewLine}{Environment.NewLine}I recorded each listed baseline before earning any XP in that class this match. "
            + "This confirms the starting value for analytics. It does not change the XP target or confirm received bonuses.";
        SetBusy(true);
        try
        {
            if (TrackerDialogs.Choose(this, onlyClass is { } trackClass ? $"Confirm {trackClass} starting XP" : "Confirm all captured starting baselines",
                message, onlyClass is { } named ? $"Confirm {named}" : "Confirm all listed baselines", "Cancel") != 0) return;
            if (tracker != owner || tracker.ActiveSession?.Id != session.Id || versions.Any(pair =>
                session.Tracks[pair.Key].Baseline is not { } baseline || (baseline.Id, baseline.Version) != pair.Value))
            { SessionNotice.Text = "A baseline changed. Review the current values before confirming again."; return; }
            var result = onlyClass is { } selected ? tracker.ConfirmCoverage(selected) : tracker.ConfirmCoverage();
            SessionNotice.Text = result.Applied ? "Starting baseline timing confirmed for analytics. Live XP checks are unchanged." : result.Message;
            if (result.Changed) await PersistTrackerAsync();
            if (result.Applied && !persistenceBlocked) await WalkthroughCoverageAsync();
            RefreshTracker();
        }
        finally { FinishWork(); }
    }
}
