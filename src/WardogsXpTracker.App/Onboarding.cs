using System.Windows;
using System.Windows.Threading;
using WardogsXpTracker.Core.Tracking;

namespace WardogsXpTracker.App;

public partial class MainWindow
{
    private bool onboardingStartupChecked;
    private bool onboardingOpen;

    private async Task ShowInitialOnboardingAsync()
    {
        if (onboardingStartupChecked || !IsVisible || closed) return;
        onboardingStartupChecked = true;
        if (!OnboardingProgress.ShouldShowAutomatically(preferences)) return;
        while (busy && !isClosing && !closed) await Task.Delay(50);
        if (!CanShowOnboarding()) return;
        onboardingOpen = true;
        SetBusy(true);
        await System.Windows.Threading.Dispatcher.Yield(DispatcherPriority.ContextIdle);
        await RunOnboardingAsync();
    }

    private async void OpenOnboarding_Click(object sender, RoutedEventArgs args) => await ShowOnboardingAsync();

    private async Task ShowOnboardingAsync()
    {
        if (!CanShowOnboarding()) return;
        onboardingOpen = true;
        SetBusy(true);
        await RunOnboardingAsync();
    }

    private bool CanShowOnboarding() => !onboardingOpen && trackerReady && !busy && !isClosing
        && !switchingSeason && !persistenceBlocked && IsVisible && !closed;

    private async Task RunOnboardingAsync()
    {
        try
        {
            var completed = OnboardingDialog.Show(this, BuildOnboardingContext(), SaveOnboardingProgressAsync);
            if (completed == true && TrackerDialogs.Choose(this, "Try guided tracking?",
                "Try the next steps with real game captures? Start before earning XP in a new match. You can pause and continue later. "
                + "For a proper Actions XP versus Bonus XP breakdown, you must confirm starting baselines and final results. Details are in Analytics.",
                "Start guided walkthrough", "Not now") == 0)
                await StartInteractiveWalkthroughAsync();
        }
        finally { onboardingOpen = false; FinishWork(); }
    }

    private OnboardingContext BuildOnboardingContext()
    {
        var source = mockScreenshotMode ? "Tracker: mock screenshot walkthrough" : "Tracker: real season data";
        if (importedAnalytics is not null) source += " · Analytics: imported report";
        else if (fakeAnalyticsSeason is not null) source += " · Analytics: mock examples";
        var session = tracker?.ActiveSession;
        var hint = resumePending
            ? "A saved match is paused. Resume Saved Session only for the same game match. Choose New Session for a different match."
            : session?.PendingFinal is not null
                ? "Saved final results await your decision. Open Saved Results to review them. New Session starts the next match."
            : session?.State == TrackerSessionState.Active
                ? "A match is already active. Continue it for the same match. Choose New Session when the game match changes."
            : "No active match. Use New Session when you are ready.";
        return new(tracker?.Document.Name ?? "Selected season", source, hint,
            ShortcutLabel(snapshotBinding), ShortcutLabel(checkBinding),
            session?.RulesSnapshot.TargetXp ?? selectedRules.TargetXp, CompletionSoundEnabled);
    }

    private async Task SaveOnboardingProgressAsync(bool completed)
    {
        if (saveStore is null || !trackerReady || isClosing || persistenceBlocked)
            throw new InvalidOperationException("Resolve the local save error before saving setup progress.");
        var oldCompleted = preferences.OnboardingCompletedVersion;
        var oldDeferred = preferences.OnboardingDeferredVersion;
        var changed = completed ? OnboardingProgress.MarkCompleted(preferences) : OnboardingProgress.MarkDeferred(preferences);
        if (!changed) return;
        try
        {
            // This guide changes only its two global progress fields, including during a mock replay.
            await saveStore.SaveAsync("settings.md", preferences);
        }
        catch
        {
            preferences.OnboardingCompletedVersion = oldCompleted;
            preferences.OnboardingDeferredVersion = oldDeferred;
            throw;
        }
    }
}
