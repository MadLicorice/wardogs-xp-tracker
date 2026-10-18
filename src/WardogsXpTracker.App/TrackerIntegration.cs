using System.Globalization;
using System.ComponentModel;
using System.IO;
using System.Media;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WardogsXpTracker.Core.Persistence;
using WardogsXpTracker.Core.Reading;
using WardogsXpTracker.Core.Tracking;
using WardogsXpTracker.Windows.Input;
using WardogsXpTracker.Windows.Reading;

namespace WardogsXpTracker.App;

public sealed record SeasonChoice(Guid Id, string Name);
public sealed class TrackerPreferences
{
    [JsonRequired] public int SchemaVersion { get; set; } = 1;
    [JsonRequired] public Guid SelectedSeasonId { get; set; }
    [JsonRequired] public List<SeasonChoice> Seasons { get; set; } = [];
    public string SnapshotKey { get; set; } = "F3";
    public string CheckKey { get; set; } = "F4";
    public bool CompletionSound { get; set; } = true;
    public bool EnableTestTools { get; set; } = false;
    public bool AutomaticReadEnabled { get; set; } = true;
    public string CaptureMode { get; set; } = "QuietVisibleClient";
    public int OnboardingCompletedVersion { get; set; }
    public int OnboardingDeferredVersion { get; set; }
    public InteractiveWalkthroughState? InteractiveWalkthrough { get; set; }
}

public sealed record TrackerCard(XpClass Class, string ClassName, bool Enabled, string StateText,
    string GainText, string BaselineText, string CurrentText, string Detail, string UpdatedText,
    string Warning, string ForecastText, double ProgressValue, string Background, string Accent,
    Visibility CheckmarkVisibility)
{
    public string SecondaryText => string.IsNullOrWhiteSpace(Warning) ? ForecastText : Warning;
    public string SecondaryTooltip => string.Join("\n", new[] { Warning, ForecastText }.Where(text => !string.IsNullOrWhiteSpace(text)));
    public string CardTooltip => string.Join("\n", new[] { StateText, Detail, Warning, ForecastText, UpdatedText }.Where(text => !string.IsNullOrWhiteSpace(text)));
}

public partial class MainWindow
{
    private SessionTracker? tracker;
    private MarkdownSaveStore? saveStore;
    private TrackerPreferences preferences = new();
    private RuleSetSnapshot selectedRules = new();
    private bool trackerReady, resumePending, refreshingTracker, switchingSeason, persistenceBlocked;
    private bool f3Registered;
    private HotkeyBinding snapshotBinding = new(Key.F3, ModifierKeys.None);
    private HotkeyBinding checkBinding = new(Key.F4, ModifierKeys.None);
    private Window? rebaseWindow;
    private bool closeApproved;
    private bool isClosing;
    private Task? trackerInitialization;
    private FileStream? trackerSaveLock;
    private sealed record TrackerCaptureContext(SessionTracker Tracker, Guid SeasonId, Guid SessionId, bool Snapshot, ReadingSource Source);

    private async Task InitializeTrackerAsync()
    {
        try
        {
            var directory = Environment.GetEnvironmentVariable("WARDOGS_XP_DATA_DIRECTORY");
            if (string.IsNullOrWhiteSpace(directory)) directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MadLicorice", "WardogsXpTracker");
            saveStore = new(directory);
            trackerSaveLock = await Task.Run(() =>
            {
                Directory.CreateDirectory(saveStore.RootDirectory);
                return new FileStream(Path.Combine(saveStore.RootDirectory, ".writer.lock"),
                    FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            });
            SavePathText.Text = saveStore.RootDirectory;
            var loaded = await saveStore.LoadAsync<TrackerPreferences>("settings.md");
            if (loaded is null)
            {
                if (await Task.Run(() => Directory.EnumerateFiles(saveStore.RootDirectory, "season-*.md").Any()))
                    throw new InvalidDataException("Settings are missing beside existing season history. Restore settings.md.bak before continuing.");
                var document = new SeasonDocument();
                preferences = new() { SelectedSeasonId = document.SeasonId, Seasons = [new(document.SeasonId, document.Name)] };
                tracker = new(document);
                await saveStore.SaveAsync(SeasonFile(document.SeasonId), document);
                await saveStore.SaveAsync("settings.md", preferences);
            }
            else
            {
                if (loaded.SchemaVersion != 1 || loaded.Seasons is null || loaded.Seasons.Count == 0
                    || loaded.Seasons.Any(s => s.Id == Guid.Empty || string.IsNullOrWhiteSpace(s.Name))
                    || loaded.Seasons.Select(s => s.Id).Distinct().Count() != loaded.Seasons.Count
                    || !loaded.Seasons.Any(s => s.Id == loaded.SelectedSeasonId))
                    throw new InvalidDataException("The settings file has unsupported or incomplete season data.");
                preferences = loaded;
                var document = await saveStore.LoadAsync<SeasonDocument>(SeasonFile(preferences.SelectedSeasonId))
                    ?? throw new InvalidDataException("The selected season save is missing. Its history was not replaced.");
                if (document.SeasonId != preferences.SelectedSeasonId)
                    throw new InvalidDataException("The selected season save contains a different season identity. No history was changed.");
                tracker = new(document);
            }
            selectedRules = await saveStore.LoadAsync<RuleSetSnapshot>("rules-oct-15-2026-v1.md") ?? new();
            if (!File.Exists(Path.Combine(saveStore.RootDirectory, "rules-oct-15-2026-v1.md")))
                await saveStore.SaveAsync("rules-oct-15-2026-v1.md", selectedRules);
            refreshingTracker = true;
            var choices = Enumerable.Range(1, 11).Select(i => $"F{i}").Concat(
                Enumerable.Range(1, 11).Select(i => $"Ctrl+F{i}")).Concat(
                Enumerable.Range(1, 11).Select(i => $"Ctrl+Shift+F{i}")).ToArray();
            SnapshotKeySelector.ItemsSource = choices;
            CheckKeySelector.ItemsSource = choices;
            SnapshotKeySelector.SelectedItem = preferences.SnapshotKey;
            CheckKeySelector.SelectedItem = preferences.CheckKey;
            SoundToggle.IsChecked = preferences.CompletionSound;
            refreshingTracker = false;
            ApplyHotkeyPreferences();
            trackerReady = true;
            resumePending = tracker.ActiveSession is { State: TrackerSessionState.Active, PendingFinal: null };
            InitializeCaptureOptions();
            tracker.CancelRebase();
            UpdateSeasonChoices();
            RefreshTracker();
            SaveStatus.Text = "Autosave enabled · Season history stays on this PC.";
        }
        catch (Exception exception)
        {
            persistenceBlocked = true;
            trackerReady = false;
            SaveStatus.Text = $"Save could not load: {exception.Message} Existing files remain unchanged.";
            RefreshSessionPresentation();
        }
        SetTrackerBusy(busy);
    }

    private async void TrackerWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (trackerInitialization is { IsCompleted: false })
        { e.Cancel = true; SessionNotice.Text = "Wait for the local save to finish loading before closing."; return; }
        if (closeApproved || !trackerReady || tracker is null) return;
        e.Cancel = true;
        if (busy) { SessionNotice.Text = "Wait for the current read or save before closing."; return; }
        SetBusy(true);
        isClosing = true;
        PauseAutomaticReading();
        rebaseWindow?.Close();
        if (HasPendingSavedResultDeletionSave && !await RetrySavedResultDeletionAsync())
        {
            if (TrackerDialogs.Choose(this, "Unsaved match deletion",
                "The match deletion could not save. Keep the app open to retry, or exit with the previous save.",
                "Keep open", "Exit with previous save") != 1)
            { isClosing = false; FinishWork(); return; }
            closeApproved = true;
            Close();
            return;
        }
        persistenceBlocked = false;
        await PersistTrackerAsync();
        var walkthroughCloseSaveFailed = !persistenceBlocked && walkthroughSavePending
            && !await SaveWalkthroughStateAsync(walkthroughPendingState);
        if (!persistenceBlocked && !walkthroughCloseSaveFailed) await PersistPreferencesAsync();
        if ((persistenceBlocked || walkthroughCloseSaveFailed) && TrackerDialogs.Choose(this, "Unsaved changes",
            walkthroughCloseSaveFailed
                ? "XP records are saved, but walkthrough progress could not save. Keep this app open to retry, or exit with the previous guide progress."
                : "The latest state could not be saved. Keep this window open to retry, or exit with the previous save.", "Keep open", "Exit with previous save") != 1)
        { isClosing = false; FinishWork(); return; }
        closeApproved = true;
        Close();
    }

    private static string SeasonFile(Guid id) => $"season-{id:N}.md";
    private TrackerCaptureContext? GetTrackerCaptureContext(bool snapshot, ReadingSource source) =>
        trackerReady && !resumePending && !persistenceBlocked && tracker?.ActiveSession is { } session
            ? new(tracker, tracker.Document.SeasonId, session.Id, snapshot, source) : null;

    private async Task ApplyTrackerReadAsync(ImageReadAnalysis analysis, TrackerCaptureContext? context)
    {
        if (context is null || tracker != context.Tracker || tracker.Document.SeasonId != context.SeasonId
            || tracker.ActiveSession?.Id != context.SessionId || persistenceBlocked) return;
        var completedBefore = tracker.ActiveSession.Tracks.Values.Count(t => t.TargetReached);
        var result = tracker.Observe(analysis.Result, context.Source, context.Snapshot, context.SessionId);
        SessionNotice.Text = result.Message;
        SetReadFeedback(result.Applied, result.Message);
        if (result.Changed) await PersistTrackerAsync();
        if (result.Applied && !persistenceBlocked) await WalkthroughReadAsync(analysis.Result, context.Snapshot);
        if (CompletionSoundEnabled && tracker.ActiveSession.Tracks.Values.Count(t => t.TargetReached) > completedBefore)
            PlayCompletionCue();
        if (rebaseWindow is not null && tracker.ActiveSession.ArmedRebase is null) rebaseWindow.Close();
        RefreshTracker();
    }

    private async Task PersistTrackerAsync(SessionTracker? owner = null)
    {
        var selected = owner ?? tracker;
        if (saveStore is null || selected is null || persistenceBlocked) return;
        try
        {
            if (mockSeasonIds.Contains(selected.Document.SeasonId))
            {
                await MockStore.SaveAsync($"mock-session-{selected.Document.SeasonId:N}.md", selected.Document);
                SaveStatus.Text = $"MOCK DATA saved separately · {DateTime.Now:T}";
                return;
            }
            await saveStore.SaveAsync(SeasonFile(selected.Document.SeasonId), selected.Document);
            SaveStatus.Text = $"Autosaved locally · {DateTime.Now:T}";
        }
        catch (Exception exception)
        {
            persistenceBlocked = true;
            PauseAutomaticReading();
            SaveStatus.Text = $"Autosave failed: {exception.Message} Keep this app open. Use Retry save.";
        }
    }

    private async Task<bool> PersistPreferencesAsync()
    {
        if (mockScreenshotMode) return true;
        if (saveStore is null || persistenceBlocked) return false;
        try { await saveStore.SaveAsync("settings.md", preferences); return true; }
        catch (Exception exception)
        {
            persistenceBlocked = true;
            PauseAutomaticReading();
            SaveStatus.Text = $"Settings save failed: {exception.Message} Keep the app open and use Retry save.";
            SetTrackerBusy(busy);
            return false;
        }
    }

    private async void RetrySave_Click(object sender, RoutedEventArgs e)
    {
        if (!trackerReady)
        {
            SessionNotice.Text = "The local save did not finish loading. Resolve the reported load error, then restart the app. Existing files remain unchanged.";
            return;
        }
        if (busy || isClosing || tracker is null) return;
        SetBusy(true);
        try
        {
            if (HasPendingSavedResultDeletionSave)
            { await RetrySavedResultDeletionAsync(); return; }
            persistenceBlocked = false;
            await PersistTrackerAsync();
            if (!persistenceBlocked) await PersistPreferencesAsync();
            RefreshTracker();
        }
        finally { FinishWork(); }
    }

    private async void NewSession_Click(object sender, RoutedEventArgs e)
    {
        if (!CanChangeTracker()) return;
        var owner = tracker!;
        SetBusy(true);
        try
        {
            var reuse = false;
            if (sender is FrameworkElement { Tag: "WalkthroughFresh" })
            {
                if (TrackerDialogs.Choose(this, "New session for guided tracking?",
                    "Start a new match record with fresh baseline snapshots? Begin before earning XP in the new game match. Existing match history stays saved.",
                    "Start new session", "Cancel") != 0) return;
            }
            else if (owner.LastKnownConfirmedFinal is not null)
            {
                var choice = TrackerDialogs.Choose(this, "New Session",
                    "Final XP from last KNOWN match is already recorded. Use it as your new baseline? Only choose Yes if these are this match's starting totals, with no unrecorded match in between.",
                    "Yes", "No, snapshot manually");
                if (choice is null) return;
                reuse = choice == 0;
            }
            else if (owner.ActiveSession is not null && TrackerDialogs.Choose(this, "New Session",
                "Start a new match record? The existing session and its history remain saved. Fresh baselines are required.", "New Session", "Cancel") != 0) return;
            rebaseWindow?.Close();
            var result = owner.StartSession(reuse, reuse, selectedRules);
            resumePending = false;
            SessionNotice.Text = result.Message;
            if (result.Changed) await PersistTrackerAsync();
            if (result.Applied && !persistenceBlocked) await WalkthroughNewSessionAsync();
            if (result.Applied && !mockScreenshotMode) RestoreAutomaticReadPreference();
            RefreshTracker();
        }
        catch (Exception exception) { SessionNotice.Text = exception.Message; }
        finally { FinishWork(); }
    }

    private void ResumeSession_Click(object sender, RoutedEventArgs e)
    {
        resumePending = false;
        SessionNotice.Text = "Saved session resumed. Use New Session if the game match changed while this app was closed.";
        RefreshTracker();
        RestoreAutomaticReadPreference();
    }

    private async void NewSnapshot_Click(object sender, RoutedEventArgs e) => await SnapshotAsync();
    private async Task SnapshotAsync()
    {
        if (busy || closed) return;
        if (!trackerReady || persistenceBlocked)
        { SessionNotice.Text = "Tracking is unavailable. Check the local save status before recording XP."; return; }
        if (resumePending)
        { SessionNotice.Text = "No Active Session. Resume the saved session or select New Session before using the snapshot key."; return; }
        if (!CanChangeTracker()) return;
        var session = tracker!.ActiveSession;
        if (session is null || session.State != TrackerSessionState.Active)
        { SessionNotice.Text = "No Active Session. Select New Session to enable baseline snapshots."; return; }
        if (session.ArmedRebase is null && !session.Tracks.Values.Any(t => t.Enabled && t.Baseline is null))
        {
            SessionNotice.Text = "Snapshots are locked. Start a New Session or use Re-Establish Baseline.";
            return;
        }
        await CaptureWindowAsync(true);
    }

    private async void TrackEnabled_Changed(object sender, RoutedEventArgs e)
    {
        if (refreshingTracker || busy || !CanChangeTracker() || sender is not CheckBox { DataContext: TrackerCard card } box) return;
        var enabled = box.IsChecked == true;
        var existing = tracker!.ActiveSession?.Tracks.GetValueOrDefault(card.Class)?.Enabled
            ?? tracker.Document.EnabledTracks.GetValueOrDefault(card.Class, true);
        if (existing == enabled) return;
        SetBusy(true);
        try { tracker.SetTrackEnabled(card.Class, enabled); await PersistTrackerAsync(); if (!persistenceBlocked) await WalkthroughCoverageAsync(); RefreshTracker(); }
        finally { FinishWork(); }
    }

    private async void Manual_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: XpClass track } || busy || !CanChangeTracker() || resumePending) return;
        if (tracker!.ActiveSession is not { } session)
        { SessionNotice.Text = "Start or resume a session before entering XP."; return; }
        SetBusy(true);
        try
        {
            var completedBefore = session.Tracks.Values.Count(t => t.TargetReached);
            var state = session.Tracks[track];
            var final = session.PendingFinal is not null;
            var closing = session.State == TrackerSessionState.Ended && session.Outcome is SessionOutcome.LeftMatch or SessionOutcome.Disconnected;
            if (session.State != TrackerSessionState.Active && !final && !closing)
            { SessionNotice.Text = "This session is frozen. Start a New Session for live XP."; return; }
            var baseline = !final && !closing && (state.Baseline is null || session.ArmedRebase == track);
            var value = TrackerDialogs.Xp(rebaseWindow ?? this, $"{track} · {(final ? "final XP" : closing ? "closing XP" : baseline ? "baseline" : "current XP")}",
                final ? "Enter the observed final XP after the match. This correction does not advance live target checks."
                : closing ? "Enter cumulative XP after leaving, before you earn XP in another match. This is a closing observation."
                : "Enter the exact cumulative XP from the game's current progression column.");
            if (value is null) return;
            var result = final ? tracker.ManualFinal(track, value.Value) : closing ? tracker.RecordClosingXp(track, value.Value)
                : baseline ? tracker.ManualBaseline(track, value.Value) : tracker.ManualCurrent(track, value.Value);
            SessionNotice.Text = result.Message;
            if (result.Changed) await PersistTrackerAsync();
            if (CompletionSoundEnabled && session.Tracks.Values.Count(t => t.TargetReached) > completedBefore)
                PlayCompletionCue();
            if (rebaseWindow is not null && session.ArmedRebase is null) rebaseWindow.Close();
            RefreshTracker();
        }
        finally { FinishWork(); }
    }

    private async void Rebase_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: XpClass track } || busy || !CanChangeTracker() || resumePending) return;
        var answer = TrackerDialogs.Choose(this, "Re-Establish Baseline",
            $"Record a new {track} baseline? The existing value stays active until a valid replacement is recorded.",
            "New measuring interval", "Old baseline was wrong", "Cancel");
        if (answer is null or 2) return;
        rebaseWindow?.Close();
        var ownerTracker = tracker!;
        var ownerSessionId = ownerTracker.ActiveSession?.Id;
        SetBusy(true);
        try
        {
            var result = ownerTracker.ArmRebase(track, answer == 1);
            SessionNotice.Text = result.Message;
            if (!result.Applied) return;
            await PersistTrackerAsync(ownerTracker);
            RefreshTracker();
        }
        finally { FinishWork(); }
        if (persistenceBlocked || tracker != ownerTracker || ownerTracker.ActiveSession?.Id != ownerSessionId
            || ownerTracker.ActiveSession?.ArmedRebase != track) return;
        var panel = new StackPanel { Margin = new Thickness(22) };
        panel.Children.Add(new TextBlock { Text = $"Record {track} XP now with {snapshotBinding}!", FontSize = 19, FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = "Return to the game and show its current XP column. This window stays armed when the game has focus. Cancel keeps the old baseline.", Margin = new Thickness(0, 12, 0, 18) });
        var manual = new Button { Content = "Enter XP manually", Tag = track, Margin = new Thickness(0, 0, 0, 8) };
        manual.Click += Manual_Click;
        panel.Children.Add(manual);
        var cancel = new Button { Content = "Cancel" };
        panel.Children.Add(cancel);
        var dialog = new Window { Owner = this, Title = $"{track} baseline", Width = 440, SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = panel, ResizeMode = ResizeMode.NoResize,
            Style = (Style)FindResource(typeof(Window)) };
        rebaseWindow = dialog;
        cancel.Click += (_, _) => dialog.Close();
        dialog.Closed += async (_, _) =>
        {
            if (rebaseWindow != dialog) return;
            rebaseWindow = null;
            if (ownerTracker.ActiveSession?.Id == ownerSessionId) ownerTracker.CancelRebase();
            await PersistTrackerAsync(ownerTracker);
            RefreshTracker();
        };
        dialog.ShowDialog();
    }

    private async void Coverage_Click(object sender, RoutedEventArgs e)
    {
        await ConfirmAnalyticsBaselinesAsync(null);
    }

    private async void ConfirmFinal_Click(object sender, RoutedEventArgs e)
    {
        if (busy || !CanChangeTracker() || tracker?.ActiveSession is not { } session) return;
        await ReviewFinalAsync(tracker, session);
    }

    private async void DeferFinal_Click(object sender, RoutedEventArgs e)
    {
        if (busy || !CanChangeTracker()) return;
        SetBusy(true);
        try
        {
            var result = tracker!.KeepPendingFinal();
            SessionNotice.Text = result.Message;
            if (result.Changed) await PersistTrackerAsync();
            RefreshTracker();
        }
        finally { FinishWork(); }
    }

    private async void EndSession_Click(object sender, RoutedEventArgs e)
    {
        if (busy || !CanChangeTracker() || resumePending) return;
        SetBusy(true);
        try
        {
            var choice = TrackerDialogs.Choose(this, "End Session", "Missing screens do not establish a bonus outcome. Left Match and Disconnected record your report of no bonus.",
                "Finished", "Left Match", "Disconnected", "Unknown");
            if (choice is null) return;
            var outcome = new[] { SessionOutcome.Finished, SessionOutcome.LeftMatch, SessionOutcome.Disconnected, SessionOutcome.Unknown }[choice.Value];
            var result = tracker!.End(outcome);
            SessionNotice.Text = result.Message;
            if (result.Changed) await PersistTrackerAsync();
            RefreshTracker();
        }
        finally { FinishWork(); }
    }

    private bool CanChangeTracker() => trackerReady && tracker is not null && !persistenceBlocked && !switchingSeason && !isClosing && !busy;

    private void RefreshTracker()
    {
        if (tracker is null) return;
        refreshingTracker = true;
        try
        {
            var session = tracker.ActiveSession;
            SeasonStatus.Text = $"{tracker.Document.Name} · {(session?.RulesSnapshot ?? selectedRules).Name}";
            SessionHeading.Text = session is null ? "No Active Session" : resumePending ? "Saved session · Waiting to resume"
                : $"{session.State} session · {session.StartedAt.ToLocalTime():g}";
            ResumePanel.Visibility = Visibility.Collapsed;
            RefreshFinalPrompts();
            ArmingStatus.Text = session is null ? "Start a New Session to enable baseline snapshots."
                : resumePending ? "Tracking is paused until you resume or start a New Session."
                : session.ArmedRebase is { } armed ? $"Record {armed} XP with {snapshotBinding}. Existing baseline remains active."
                : session?.State == TrackerSessionState.Active ? $"{snapshotBinding}: missing baselines only · {checkBinding}: current XP · New Session for every match"
                : "Live counters are frozen outside an active session. Final bonuses never advance the check.";
            var rules = session?.RulesSnapshot ?? selectedRules;
            RulesText.Text = $"{selectedRules.Name} · {selectedRules.TargetXp:N0} XP target for new sessions. Each session keeps its exact rules. Rules file: rules-oct-15-2026-v1.md";
            TrackerCards.ItemsSource = Enum.GetValues<XpClass>().Select(track => CreateCard(track, session, rules)).ToArray();
            RefreshSessionPresentation();
            RefreshAnalyticsIfVisible();
            RefreshSavedResults();
        }
        finally { refreshingTracker = false; }
        SetTrackerBusy(busy);
    }

    private TrackerCard CreateCard(XpClass track, TrackerSession? session, RuleSetSnapshot rules)
    {
        var state = session?.Tracks[track];
        var enabled = state?.Enabled ?? tracker!.Document.EnabledTracks.GetValueOrDefault(track, true);
        var gain = state?.MeasuredGain;
        var complete = state?.TargetReached == true;
        var tier = rules.TierFor(gain ?? 0);
        var accent = !enabled ? "#667180" : state?.Baseline is null ? "#EF8A8A" : complete ? "#7AE3A7" : gain is > 0 ? "#E8D57A" : "#AAB5C5";
        var stage = rules.Tiers.IndexOf(tier) / (double)Math.Max(1, rules.Tiers.Count - 1);
        var tierColor = $"#{(int)(62 - stage * 25):X2}{(int)(53 + stage * 12):X2}{(int)(38 + stage * 9):X2}";
        var background = !enabled ? "#1B2028" : state?.Baseline is null ? "#302127" : complete ? "#173F30"
            : gain is > 0 ? tierColor : "#242C37";
        var status = !enabled ? "Tracking disabled" : state?.Baseline is null ? "Baseline not established"
            : complete ? "Maximum bonus tier reached" : gain is > 0 ? $"Tier {rules.Tiers.IndexOf(tier)} · In progress" : "Baseline ready";
        var current = state?.Current ?? state?.LatestReading;
        if (session?.State is TrackerSessionState.Frozen or TrackerSessionState.Ended) status += " · Frozen";
        var detail = current is null ? "No accepted reading" : $"{(current.CurrentLevel is { } observedLevel ? $"Level {observedLevel} · " : "")}{(current.NextThreshold is { } observedThreshold ? $"Next threshold {observedThreshold:N0}" : "Manual XP")}";
        var finals = session?.PendingFinal ?? session?.ConfirmedFinal;
        if (finals is not null) detail = finals.Tracks.TryGetValue(track, out var final)
            ? $"{(finals.ConfirmedAt is null ? "Final candidate" : "Confirmed final")} {final.Xp:N0} XP" : "Final XP unavailable";
        else if (state?.ClosingXp is { } closing) detail = $"Closing XP {closing.Xp:N0} · live check unchanged";
        var warning = state?.WasRebased == true ? "XP rebased mid match. May not be accurate" : "";
        if (finals?.ConflictingTracks.Contains(track) == true) warning += " Final totals conflict. Use Manual XP to correct.";
        return new(track, track.ToString().ToUpperInvariant(), enabled, status,
            gain is { } measured && enabled ? $"{measured:N0} / {rules.TargetXp:N0} XP" : "—",
            state?.Baseline is { } baseline ? $"Baseline  {baseline.Xp:N0}" : "Baseline  —",
            current is { } value ? $"Current  {value.Xp:N0}" : "Current  —",
            detail,
            current is null ? "" : $"Updated {current.At.ToLocalTime():T}",
            warning,
            session?.State == TrackerSessionState.Active ? Forecast(state, rules) : "", rules.TargetXp > 0 ? Math.Clamp((gain ?? 0) * 100d / rules.TargetXp, 0, 100) : 0,
            background, accent, complete && enabled ? Visibility.Visible : Visibility.Collapsed);
    }

    private static string Forecast(TrackState? state, RuleSetSnapshot rules)
    {
        if (state is not { Enabled: true, Current: { NextThreshold: { } threshold, ThresholdLevel: { } level } current, MeasuredGain: { } gain }) return "";
        var supportedGain = state.OriginalBaseline is { Validity: BaselineValidity.Valid, Coverage: BaselineCoverage.BeforeEarnedXp } original
            ? Math.Max(0, current.Xp - original.Xp) : gain;
        var bonus = rules.TierFor(supportedGain);
        if (current.Xp + bonus.Third >= threshold) return $"At least Lv {level} with 3rd or better · estimate";
        if (current.Xp + bonus.Second >= threshold) return $"At least Lv {level} with 2nd or better · estimate";
        if (current.Xp + bonus.First >= threshold) return $"At least Lv {level} with 1st · estimate";
        return "";
    }

    private void SetTrackerBusy(bool value)
    {
        var available = trackerReady && !persistenceBlocked && !switchingSeason && !value;
        UpdateTestControls(available);
        RecordFinalButton.IsEnabled = available && tracker?.ActiveSession?.PendingFinal?.Tracks.Count > 0;
        DeferFinalButton.IsEnabled = available;
        SavedResultsGrid.IsEnabled = trackerReady && !value && !switchingSeason && !isClosing && !HasPendingSavedResultDeletionSave;
        NewSessionButton.IsEnabled = available;
        var active = tracker?.ActiveSession is { State: TrackerSessionState.Active };
        SnapshotButton.IsEnabled = available && !resumePending && active &&
            (tracker!.ActiveSession!.ArmedRebase is not null || tracker.ActiveSession.Tracks.Values.Any(t => t.Enabled && t.Baseline is null));
        EndSessionButton.IsEnabled = available && !resumePending && tracker?.ActiveSession is not null;
        ConfirmAllCoverageButton.IsEnabled = available && !resumePending && tracker?.ActiveSession?.Tracks.Values.Any(CanConfirmStartingBaseline) == true;
        CoverageCards.IsEnabled = available && !resumePending;
        var cardsAvailable = available && !resumePending && tracker?.ActiveSession is not null;
        TrackerCardSurface.IsEnabled = cardsAvailable;
        TrackerCards.IsEnabled = cardsAvailable;
        StartSessionOverlayButton.IsEnabled = available;
        ResumeOverlayButton.IsEnabled = available;
        RetrySaveButton.Visibility = persistenceBlocked && trackerReady ? Visibility.Visible : Visibility.Collapsed;
        RetrySaveButton.IsEnabled = !value && !isClosing;
        SeasonSelector.IsEnabled = available && !mockScreenshotMode;
        SeasonNameBox.IsEnabled = available && !mockScreenshotMode;
        AddSeasonButton.IsEnabled = available && !mockScreenshotMode;
        RenameSeasonButton.IsEnabled = available && !mockScreenshotMode;
        SoundToggle.IsEnabled = available && !mockScreenshotMode;
        SnapshotKeySelector.IsEnabled = available && !mockScreenshotMode;
        CheckKeySelector.IsEnabled = available && !mockScreenshotMode;
        ApplyHotkeysButton.IsEnabled = available && !mockScreenshotMode;
        CaptureModeSelector.IsEnabled = available && !mockScreenshotMode;
        AutoReadToggle.IsEnabled = available && !mockScreenshotMode;
        OpenOnboardingButton.IsEnabled = available && !isClosing;
        RefreshInteractivePresentation();
        OptionsTab.IsEnabled = true;
    }

    private void UpdateSeasonChoices()
    {
        refreshingTracker = true;
        SeasonSelector.ItemsSource = preferences.Seasons.ToArray();
        SeasonSelector.SelectedItem = preferences.Seasons.FirstOrDefault(s => s.Id == preferences.SelectedSeasonId);
        SeasonNameBox.Text = tracker?.Document.Name ?? "";
        refreshingTracker = false;
    }

    private async void Season_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (mockScreenshotMode) return;
        if (refreshingTracker || busy || switchingSeason || !CanChangeTracker() || SeasonSelector.SelectedItem is not SeasonChoice choice
            || choice.Id == tracker!.Document.SeasonId) return;
        switchingSeason = true;
        SetBusy(true);
        try
        {
            rebaseWindow?.Close();
            await PersistTrackerAsync();
            if (persistenceBlocked) return;
            var document = await saveStore!.LoadAsync<SeasonDocument>(SeasonFile(choice.Id))
                ?? throw new InvalidDataException("The season save is missing. Existing history was not replaced.");
            var replacement = new SessionTracker(document);
            if (replacement.Document.SeasonId != choice.Id) throw new InvalidDataException("The season save identity does not match.");
            var previousId = preferences.SelectedSeasonId;
            preferences.SelectedSeasonId = choice.Id;
            if (!await PersistPreferencesAsync()) { preferences.SelectedSeasonId = previousId; return; }
            tracker = replacement;
            resumePending = tracker.ActiveSession is { State: TrackerSessionState.Active, PendingFinal: null };
            SessionNotice.Text = "Season changed. XP never carries automatically between seasons.";
        }
        catch (Exception exception) { SaveStatus.Text = exception.Message; }
        finally { switchingSeason = false; UpdateSeasonChoices(); RefreshTracker(); FinishWork(); }
    }

    private async void AddSeason_Click(object sender, RoutedEventArgs e)
    {
        if (mockScreenshotMode) return;
        if (busy || !CanChangeTracker()) return;
        var name = SeasonNameBox.Text.Trim();
        if (name.Length is < 1 or > 80) { SaveStatus.Text = "Enter a season name with 1 to 80 characters."; return; }
        SetBusy(true);
        try
        {
            await PersistTrackerAsync();
            if (persistenceBlocked) return;
            var document = new SeasonDocument { Name = name };
            await saveStore!.SaveAsync(SeasonFile(document.SeasonId), document);
            var previousId = preferences.SelectedSeasonId;
            var createdChoice = new SeasonChoice(document.SeasonId, name);
            preferences.Seasons.Add(createdChoice);
            preferences.SelectedSeasonId = document.SeasonId;
            if (!await PersistPreferencesAsync())
            { preferences.Seasons.Remove(createdChoice); preferences.SelectedSeasonId = previousId; return; }
            tracker = new(document);
            resumePending = false;
            UpdateSeasonChoices(); RefreshTracker();
        }
        catch (Exception exception) { SaveStatus.Text = exception.Message; }
        finally { FinishWork(); }
    }

    private async void RenameSeason_Click(object sender, RoutedEventArgs e)
    {
        if (mockScreenshotMode) return;
        if (busy || !CanChangeTracker()) return;
        var name = SeasonNameBox.Text.Trim();
        if (name.Length is < 1 or > 80) { SaveStatus.Text = "Enter a season name with 1 to 80 characters."; return; }
        SetBusy(true);
        try
        {
            tracker!.Document.Name = name;
            preferences.Seasons = preferences.Seasons.Select(s => s.Id == tracker.Document.SeasonId ? s with { Name = name } : s).ToList();
            await PersistTrackerAsync();
            if (!persistenceBlocked) await PersistPreferencesAsync();
            UpdateSeasonChoices(); RefreshTracker();
        }
        finally { FinishWork(); }
    }

    private void ApplyHotkeyPreferences()
    {
        if (hotkeys is null) return;
        var okay = hotkeys.TryRegisterPair(ParseBinding(preferences.SnapshotKey), ParseBinding(preferences.CheckKey), out var error);
        var actualSnapshot = hotkeys.GetBinding(HotkeyAction.Snapshot);
        var actualCheck = hotkeys.GetBinding(HotkeyAction.Check);
        f3Registered = actualSnapshot is not null;
        f4Registered = actualCheck is not null;
        snapshotBinding = actualSnapshot ?? ParseBinding(preferences.SnapshotKey);
        checkBinding = actualCheck ?? ParseBinding(preferences.CheckKey);
        HotkeyStatus.Text = okay ? $"Snapshot: {snapshotBinding} · Check: {checkBinding}. Shortcuts work while the game has focus."
            : $"Global shortcut conflict: {error} Use the capture buttons or change shortcuts in Options.";
        if (!okay) SessionNotice.Text = HotkeyStatus.Text;
        SnapshotButton.Content = $"Snapshot · {ShortcutLabel(snapshotBinding)}";
        CaptureButton.Content = $"Check XP · {ShortcutLabel(checkBinding)}";
    }

    private static string ShortcutLabel(HotkeyBinding binding) => string.Join("+", new[]
    {
        binding.Modifiers.HasFlag(ModifierKeys.Control) ? "Ctrl" : null,
        binding.Modifiers.HasFlag(ModifierKeys.Shift) ? "Shift" : null,
        binding.Modifiers.HasFlag(ModifierKeys.Alt) ? "Alt" : null,
        binding.Modifiers.HasFlag(ModifierKeys.Windows) ? "Win" : null,
        binding.Key.ToString()
    }.Where(part => part is not null));

    private static HotkeyBinding ParseBinding(string text)
    {
        var parts = text.Split('+');
        if (!Enum.TryParse<Key>(parts[^1], out var key) || key is Key.None or Key.System or Key.F12)
            throw new InvalidDataException("The saved shortcut is invalid. Choose a supported shortcut in settings.md.");
        var modifiers = (text.Contains("Ctrl+", StringComparison.Ordinal) ? ModifierKeys.Control : ModifierKeys.None)
            | (text.Contains("Shift+", StringComparison.Ordinal) ? ModifierKeys.Shift : ModifierKeys.None);
        return new(key, modifiers);
    }

    private async void Sound_Changed(object sender, RoutedEventArgs e)
    {
        if (mockScreenshotMode) return;
        if (refreshingTracker || !CanChangeTracker() || saveStore is null) return;
        preferences.CompletionSound = SoundToggle.IsChecked == true;
        await PersistPreferencesAsync();
    }

    private async void ApplyHotkeys_Click(object sender, RoutedEventArgs e)
    {
        if (mockScreenshotMode) return;
        if (!CanChangeTracker() || busy) return;
        var snapshot = SnapshotKeySelector.SelectedItem as string ?? "F3";
        var check = CheckKeySelector.SelectedItem as string ?? "F4";
        if (snapshot == check) { SessionNotice.Text = "Snapshot and Check must use different shortcuts."; return; }
        if (hotkeys is null) return;
        if (!hotkeys.TryRegisterPair(ParseBinding(snapshot), ParseBinding(check), out var error))
        { ApplyHotkeyPreferences(); SessionNotice.Text = error; return; }
        preferences.SnapshotKey = snapshot;
        preferences.CheckKey = check;
        preferences.CompletionSound = SoundToggle.IsChecked == true;
        await PersistPreferencesAsync();
        ApplyHotkeyPreferences(); RefreshTracker();
    }
}
