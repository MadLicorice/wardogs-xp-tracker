using System.Windows;
using System.Windows.Controls;
using WardogsXpTracker.Core.Reading;
using WardogsXpTracker.Core.Tracking;
using WardogsXpTracker.Windows.Capture;

namespace WardogsXpTracker.App;

public partial class MainWindow
{
    private bool walkthroughSavePending;
    private InteractiveWalkthroughState? walkthroughPendingState;
    private string walkthroughSaveError = "";
    private bool IsRealWalkthroughTracker => tracker is not null && !mockScreenshotMode
        && !mockSeasonIds.Contains(tracker.Document.SeasonId);

    private async void OpenInteractiveWalkthrough_Click(object sender, RoutedEventArgs args)
    {
        if (!CanChangeTracker() || onboardingOpen) return;
        SetBusy(true);
        try { await StartInteractiveWalkthroughAsync(); }
        finally { FinishWork(); }
    }

    private async Task StartInteractiveWalkthroughAsync()
    {
        if (walkthroughSavePending)
        {
            await SaveWalkthroughStateAsync(walkthroughPendingState);
            MainTabs.SelectedItem = SessionTrackerTab;
            TrackerSidebar.ScrollToTop();
            return;
        }
        var current = preferences.InteractiveWalkthrough;
        var next = current is null || !InteractiveWalkthroughProgress.IsSupported(current) || current.Stage == InteractiveWalkthroughStep.Completed
            ? InteractiveWalkthroughProgress.Start(DateTimeOffset.UtcNow)
            : ResumeInteractiveWalkthrough(current);
        await SaveWalkthroughStateAsync(next);
        MainTabs.SelectedItem = SessionTrackerTab;
        TrackerSidebar.ScrollToTop();
        RefreshInteractivePresentation();
    }

    private InteractiveWalkthroughState ResumeInteractiveWalkthrough(InteractiveWalkthroughState state)
    {
        var resumed = InteractiveWalkthroughProgress.Resume(state);
        if (!IsRealWalkthroughTracker || tracker is null) return resumed;
        if (resumed.Stage == InteractiveWalkthroughStep.StartNewSession && tracker.ActiveSession is { } newSession)
            resumed = InteractiveWalkthroughProgress.ObserveNewSession(resumed, tracker.Document.SeasonId, newSession, true);
        return tracker.Document.Sessions.FirstOrDefault(session => session.Id == resumed.SessionId) is { } boundSession
            ? InteractiveWalkthroughProgress.ReconcileRetainedCaptures(resumed, tracker.Document.SeasonId, boundSession, true)
            : resumed;
    }

    private async Task<bool> SaveWalkthroughStateAsync(InteractiveWalkthroughState? next)
    {
        if (saveStore is null || persistenceBlocked || !trackerReady) return false;
        var previous = preferences.InteractiveWalkthrough;
        if (!walkthroughSavePending && Equals(previous, next)) return true;
        preferences.InteractiveWalkthrough = next;
        try
        {
            await saveStore.SaveAsync("settings.md", preferences);
            walkthroughSavePending = false; walkthroughPendingState = null; walkthroughSaveError = "";
            return true;
        }
        catch (Exception exception)
        {
            preferences.InteractiveWalkthrough = previous;
            walkthroughSavePending = true; walkthroughPendingState = next;
            walkthroughSaveError = $"Walkthrough progress could not save: {exception.Message} Use Retry walkthrough save. XP records are unchanged.";
            return false;
        }
        finally { RefreshInteractivePresentation(); }
    }

    private async Task AdvanceWalkthroughAsync(Func<InteractiveWalkthroughState, InteractiveWalkthroughState> observe)
    {
        if (walkthroughSavePending || preferences.InteractiveWalkthrough is not { } current || current.IsPaused || persistenceBlocked) return;
        var next = observe(current);
        if (!Equals(next, current)) await SaveWalkthroughStateAsync(next);
    }

    private Task WalkthroughNewSessionAsync() => tracker?.ActiveSession is not { } session ? Task.CompletedTask
        : AdvanceWalkthroughAsync(state => InteractiveWalkthroughProgress.ObserveNewSession(
            InteractiveWalkthroughProgress.ObserveWindowSelected(state, IsRealWalkthroughTracker && WindowSelector.SelectedItem is CaptureTarget),
            tracker.Document.SeasonId, session, IsRealWalkthroughTracker));

    private Task WalkthroughReadAsync(ScreenReadResult read, bool snapshot) => tracker?.ActiveSession is not { } session ? Task.CompletedTask
        : AdvanceWalkthroughAsync(state => InteractiveWalkthroughProgress.ObserveAcceptedRead(state,
            tracker.Document.SeasonId, session, read, IsRealWalkthroughTracker, snapshot));

    private Task WalkthroughCoverageAsync() => tracker?.ActiveSession is not { } session ? Task.CompletedTask
        : AdvanceWalkthroughAsync(state => InteractiveWalkthroughProgress.CheckCoverage(state, tracker.Document.SeasonId, session));

    private Task WalkthroughFinalConfirmedAsync(SessionTracker owner, TrackerSession session) =>
        !ReferenceEquals(owner, tracker) || !IsRealWalkthroughTracker ? Task.CompletedTask
        : AdvanceWalkthroughAsync(state => InteractiveWalkthroughProgress.ObserveFinalConfirmed(state, owner.Document.SeasonId, session));

    private void RefreshInteractivePresentation()
    {
        if (InteractiveWalkthroughPanel is null) return;
        var state = preferences.InteractiveWalkthrough ?? walkthroughPendingState;
        InteractiveWalkthroughPanel.Visibility = state is null && !walkthroughSavePending ? Visibility.Collapsed : Visibility.Visible;
        OpenInteractiveWalkthroughButton.Content = state is null || state.Stage == InteractiveWalkthroughStep.Completed
            ? "Start guided walkthrough" : "Resume guided walkthrough";
        if (walkthroughSavePending) OpenInteractiveWalkthroughButton.Content = "Retry walkthrough save";
        var available = trackerReady && !busy && !isClosing && !persistenceBlocked && !switchingSeason;
        OpenInteractiveWalkthroughButton.IsEnabled = available;
        WalkthroughPrimaryButton.IsEnabled = available;
        WalkthroughPauseButton.IsEnabled = available && !walkthroughSavePending && state?.Stage != InteractiveWalkthroughStep.Completed;
        WalkthroughRestartButton.IsEnabled = available && !walkthroughSavePending;
        WalkthroughCloseButton.IsEnabled = available && !walkthroughSavePending;
        WalkthroughSkipButton.IsEnabled = available && !walkthroughSavePending && state is { IsPaused: false }
            && state.Stage != InteractiveWalkthroughStep.Completed && InteractiveWalkthroughProgress.IsSupported(state);
        WalkthroughPauseButton.Content = state?.IsPaused == true ? "Continue guide" : "Pause guide";
        if (state is null) return;
        WalkthroughStepTitle.Text = state.Stage == InteractiveWalkthroughStep.Completed ? state.HasSkippedSteps ? "Walkthrough finished · steps skipped" : "Walkthrough complete"
            : $"Guided tracking · {(int)state.Stage + 1}/8" + (state.Class is { } trackClass ? $" · {trackClass}" : "");
        WalkthroughCertificationNotice.Text = "For an accurate Actions XP versus Bonus XP breakdown, you MUST confirm starting baselines and final results. Details are in Analytics.";
        WalkthroughPrimaryButton.Content = "Continue";
        WalkthroughMessage.Text = state.Stage switch
        {
            InteractiveWalkthroughStep.SelectGameWindow => "Select WARDOGS under GAME WINDOW, then Continue. This walkthrough uses real game captures. Start before earning XP in a new match.",
            InteractiveWalkthroughStep.StartNewSession => "Select New Session for a new game match. Choose Track for the classes you want. Already playing? Pause this guide until the next match.",
            InteractiveWalkthroughStep.CaptureBaseline => $"Before earning XP, open TAB → UNLOCK → a tracked class's current XP column. Push {ShortcutLabel(snapshotBinding)}. Completed reward pages do not show current XP. Use fresh snapshots for this walkthrough.",
            InteractiveWalkthroughStep.ConfirmStartingCoverage => "Capture baselines for every class you track, or uncheck Track on classes you do not need. Confirm only starts captured before earning XP. If you already earned XP first, pause until a fresh match.",
            InteractiveWalkthroughStep.CheckCurrentXp => $"Show {(state.Class is { } currentClass ? currentClass.ToString() : "a tracked class")}'s current UNLOCK column again and push {ShortcutLabel(checkBinding)}. A valid real read completes this step even if XP has not increased. A green check marks the maximum earned-XP tier, separate from bonuses.",
            InteractiveWalkthroughStep.CaptureFinalXp => $"At match end, show PROGRESSION and push {ShortcutLabel(checkBinding)}. The walkthrough needs a real final reading for {(state.Class is { } finalClass ? finalClass.ToString() : "a tracked class")}. You can pause while you play. Use End Session if you leave or disconnect.",
            InteractiveWalkthroughStep.ConfirmFinalXp => "Record Final XP to confirm the captured totals. Choose only a placement you know. Decide Later keeps the evidence in Saved Results. Confirm all tracked final totals for complete analytics.",
            InteractiveWalkthroughStep.OpenAnalytics => "Your final result is recorded. Open this season's real Analytics to inspect Actions XP, Bonus XP, Indeterminate XP, and missed-bonus estimates. Unknown sources stay indeterminate.",
            _ => "Open Analytics for the breakdown. Start a new session for each game match. You can close this guide without ending your session."
        };
        WalkthroughPrimaryButton.Content = state.Stage switch
        {
            InteractiveWalkthroughStep.StartNewSession => "New session…",
            InteractiveWalkthroughStep.CaptureBaseline => $"Snapshot · {ShortcutLabel(snapshotBinding)}",
            InteractiveWalkthroughStep.ConfirmStartingCoverage => "Confirm starting baselines…",
            InteractiveWalkthroughStep.CheckCurrentXp or InteractiveWalkthroughStep.CaptureFinalXp => $"Read XP · {ShortcutLabel(checkBinding)}",
            InteractiveWalkthroughStep.ConfirmFinalXp => "Review final XP…",
            InteractiveWalkthroughStep.OpenAnalytics or InteractiveWalkthroughStep.Completed => "View real Analytics",
            _ => "Window selected · Continue"
        };
        if (state.HasSkippedSteps)
        {
            if (state.Stage == InteractiveWalkthroughStep.OpenAnalytics)
                WalkthroughMessage.Text = "Open this season's real Analytics. Skipped tasks did not confirm starting coverage or final results. Missing data remains excluded or indeterminate.";
            else if (state.Stage == InteractiveWalkthroughStep.Completed)
                WalkthroughMessage.Text = "Walkthrough finished with skipped tasks. Skipping did not capture XP, certify starting baselines, or confirm final results. Analytics can remain incomplete.";
            WalkthroughMessage.Text += " Skipped: " + string.Join(", ", state.SkippedSteps.Select(WalkthroughTaskName)) + ".";
        }
        var deletedBoundSession = IsRealWalkthroughTracker && tracker is { } selectedTracker && state.SeasonId == selectedTracker.Document.SeasonId
            && state.SessionId is { } savedId && !selectedTracker.Document.Sessions.Any(session => session.Id == savedId);
        if (state.IsPaused)
        { WalkthroughMessage.Text = "Walkthrough paused. Tracking continues normally. Continue the guide when the game is ready."; WalkthroughPrimaryButton.Content = "Continue guide"; }
        else if (!IsRealWalkthroughTracker)
        { WalkthroughMessage.Text = "Real game captures are required. Return to real data to continue. Mock screenshots and imported images do not complete these steps."; WalkthroughPrimaryButton.Content = "Return to real data"; }
        else if (state.SeasonId is { } seasonId && tracker?.Document.SeasonId != seasonId)
        { WalkthroughMessage.Text = "This walkthrough belongs to another season. Select that season in Options, or restart the guide for a new match."; WalkthroughPrimaryButton.IsEnabled = false; }
        else if (state.SessionId is { } id && (int)state.Stage < (int)InteractiveWalkthroughStep.ConfirmFinalXp
            && tracker?.ActiveSession?.Id != id)
        { WalkthroughMessage.Text = "The active match changed. Restart the guide before a new match. Earlier results remain in Saved Results."; WalkthroughPrimaryButton.IsEnabled = false; }
        else if (state.SessionId is not null && (int)state.Stage < (int)InteractiveWalkthroughStep.ConfirmFinalXp && resumePending)
        { WalkthroughMessage.Text = "The saved match is paused. Resume Saved Session only if it is still the same game match. Otherwise restart for a new match."; WalkthroughPrimaryButton.Content = "Resume saved session"; }
        else if (state.Class is { } boundClass && (int)state.Stage >= (int)InteractiveWalkthroughStep.ConfirmStartingCoverage && (int)state.Stage < (int)InteractiveWalkthroughStep.ConfirmFinalXp
            && tracker?.ActiveSession?.Tracks.GetValueOrDefault(boundClass) is { } track
            && (track.WasRebased || track.Baseline?.Id != state.BaselineId || track.Baseline?.Version != state.BaselineVersion || !track.Enabled))
        { WalkthroughMessage.Text = "This walkthrough's class was disabled or its baseline changed. Re-enable the same original track, or restart before a new match."; WalkthroughPrimaryButton.IsEnabled = false; }
        if (deletedBoundSession)
        {
            WalkthroughStepTitle.Text = "Walkthrough match deleted";
            WalkthroughMessage.Text = "This walkthrough's saved match was deleted. Restart for a new match, skip this task, or close the guide. No XP or confirmations were restored.";
            WalkthroughPrimaryButton.IsEnabled = false;
        }
        if (!InteractiveWalkthroughProgress.IsSupported(state))
        { WalkthroughMessage.Text = "This saved walkthrough needs a restart. Restart the guide for a new match. Your XP records stay unchanged."; WalkthroughPrimaryButton.IsEnabled = false; WalkthroughPauseButton.IsEnabled = false; }
        if (walkthroughSavePending)
        { WalkthroughMessage.Text = walkthroughSaveError; WalkthroughPrimaryButton.Content = "Retry walkthrough save"; WalkthroughPrimaryButton.IsEnabled = available; }
    }

    private async void WalkthroughPrimary_Click(object sender, RoutedEventArgs args)
    {
        if (!CanChangeTracker() || preferences.InteractiveWalkthrough is null && !walkthroughSavePending) return;
        if (walkthroughSavePending)
        {
            SetBusy(true);
            try { await SaveWalkthroughStateAsync(walkthroughPendingState); }
            finally { FinishWork(); }
            return;
        }
        var state = preferences.InteractiveWalkthrough!;
        if (IsRealWalkthroughTracker && tracker is { } realOwner && state.SeasonId == realOwner.Document.SeasonId && state.SessionId is { } deletedId
            && !realOwner.Document.Sessions.Any(session => session.Id == deletedId)) return;
        if (state.IsPaused) { WalkthroughPause_Click(sender, args); return; }
        if (!InteractiveWalkthroughProgress.IsSupported(state)) return;
        if (!IsRealWalkthroughTracker)
        {
            SetBusy(true);
            try { await RestoreRealDataAsync(allowDiscardPrompt: false); }
            finally { FinishWork(); }
            return;
        }
        if (state.SeasonId is { } seasonId && tracker?.Document.SeasonId != seasonId) return;
        if (state.SessionId is { } boundId && (int)state.Stage < (int)InteractiveWalkthroughStep.ConfirmFinalXp && tracker?.ActiveSession?.Id != boundId) return;
        if (state.SessionId is not null && (int)state.Stage < (int)InteractiveWalkthroughStep.ConfirmFinalXp && resumePending)
        { ResumeSession_Click(sender, args); return; }
        switch (state.Stage)
        {
            case InteractiveWalkthroughStep.SelectGameWindow:
                SetBusy(true);
                try
                {
                    if (WindowSelector.SelectedItem is not CaptureTarget) { WalkthroughMessage.Text = "Choose the WARDOGS game window first. Use Refresh if it is missing."; return; }
                    await AdvanceWalkthroughAsync(current =>
                    {
                        var selected = InteractiveWalkthroughProgress.ObserveWindowSelected(current, true);
                        return tracker?.ActiveSession is { } currentSession
                            ? InteractiveWalkthroughProgress.ObserveNewSession(selected, tracker.Document.SeasonId, currentSession, IsRealWalkthroughTracker)
                            : selected;
                    });
                }
                finally { FinishWork(); }
                break;
            case InteractiveWalkthroughStep.StartNewSession: NewSession_Click(new Button { Tag = "WalkthroughFresh" }, args); break;
            case InteractiveWalkthroughStep.CaptureBaseline: await SnapshotAsync(); break;
            case InteractiveWalkthroughStep.ConfirmStartingCoverage:
                await ConfirmAnalyticsBaselinesAsync(null);
                SetBusy(true);
                try { await WalkthroughCoverageAsync(); }
                finally { FinishWork(); }
                break;
            case InteractiveWalkthroughStep.CheckCurrentXp:
            case InteractiveWalkthroughStep.CaptureFinalXp: await CaptureWindowAsync(); break;
            case InteractiveWalkthroughStep.ConfirmFinalXp:
                var candidate = tracker?.Document.Sessions.FirstOrDefault(item => item.Id == state.SessionId)
                    ?? (state.SessionId is null && state.SkippedSteps.Contains(InteractiveWalkthroughStep.StartNewSession)
                        && tracker?.ActiveSession is { } fresh && fresh.StartedAt >= state.StartedAt ? fresh : null);
                if (candidate?.PendingFinal is not null) await ReviewFinalAsync(tracker!, candidate);
                else WalkthroughMessage.Text = "No pending final XP is available for this guide. Read the match-end PROGRESSION screen, review Saved Results, or skip this task. Nothing was confirmed.";
                break;
            case InteractiveWalkthroughStep.OpenAnalytics:
            case InteractiveWalkthroughStep.Completed:
                var recorded = tracker?.Document.Sessions.FirstOrDefault(item => item.Id == state.SessionId);
                if (!state.HasSkippedSteps && recorded?.ConfirmedFinal is null)
                { WalkthroughMessage.Text = "Confirm this match's final results in Saved Results first."; return; }
                if (!state.HasSkippedSteps && recorded is not null
                    && recorded.Tracks.Values.Where(track => track.Enabled).Any(track => !StartingBaselineConfirmed(track)))
                {
                    SetBusy(true);
                    try { ShowSavedBaselineCoverage(tracker!, recorded); }
                    finally { FinishWork(); }
                    return;
                }
                SetBusy(true);
                try
                {
                    if (!await RestoreRealDataAsync(allowDiscardPrompt: false)) return;
                    ClearImportedAnalytics();
                    MainTabs.SelectedItem = AnalyticsTab; RefreshAnalyticsIfVisible();
                    await AdvanceWalkthroughAsync(current => InteractiveWalkthroughProgress.ObserveAnalyticsOpened(current,
                        tracker!.Document.SeasonId, state.SessionId, !AnalyticsSourceIsMock && importedAnalytics is null));
                }
                finally { FinishWork(); }
                break;
        }
    }

    private async void WalkthroughSkip_Click(object sender, RoutedEventArgs args)
    {
        if (!CanChangeTracker() || walkthroughSavePending || preferences.InteractiveWalkthrough is not { IsPaused: false } state
            || state.Stage == InteractiveWalkthroughStep.Completed || !InteractiveWalkthroughProgress.IsSupported(state)) return;
        SetBusy(true);
        try { await SaveWalkthroughStateAsync(InteractiveWalkthroughProgress.SkipStep(state)); }
        finally { FinishWork(); }
    }

    private static string WalkthroughTaskName(InteractiveWalkthroughStep step) => step switch
    {
        InteractiveWalkthroughStep.SelectGameWindow => "game window",
        InteractiveWalkthroughStep.StartNewSession => "new session",
        InteractiveWalkthroughStep.CaptureBaseline => "baseline capture",
        InteractiveWalkthroughStep.ConfirmStartingCoverage => "starting coverage",
        InteractiveWalkthroughStep.CheckCurrentXp => "current XP",
        InteractiveWalkthroughStep.CaptureFinalXp => "final capture",
        InteractiveWalkthroughStep.ConfirmFinalXp => "final confirmation",
        _ => "Analytics"
    };

    private async void WalkthroughPause_Click(object sender, RoutedEventArgs args)
    {
        if (!CanChangeTracker() || walkthroughSavePending || preferences.InteractiveWalkthrough is not { } state) return;
        SetBusy(true);
        try { await SaveWalkthroughStateAsync(state.IsPaused ? ResumeInteractiveWalkthrough(state) : InteractiveWalkthroughProgress.Pause(state)); }
        finally { FinishWork(); }
    }

    private async void WalkthroughRestart_Click(object sender, RoutedEventArgs args)
    {
        if (!CanChangeTracker() || walkthroughSavePending || preferences.InteractiveWalkthrough is not { } state) return;
        SetBusy(true);
        try
        {
            if (TrackerDialogs.Choose(this, "Restart guided walkthrough?", "Restart the guide for a new match? Your current session and saved XP stay unchanged.", "Restart guide", "Cancel") != 0) return;
            await SaveWalkthroughStateAsync(InteractiveWalkthroughProgress.IsSupported(state)
                ? InteractiveWalkthroughProgress.RestartForNextMatch(state, DateTimeOffset.UtcNow) : InteractiveWalkthroughProgress.Start(DateTimeOffset.UtcNow));
        }
        finally { FinishWork(); }
    }

    private async void WalkthroughClose_Click(object sender, RoutedEventArgs args)
    {
        if (!CanChangeTracker() || walkthroughSavePending) return;
        SetBusy(true);
        try { await SaveWalkthroughStateAsync(null); }
        finally { FinishWork(); }
    }
}
