using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using WardogsXpTracker.App.Testing;
using WardogsXpTracker.Core.Analytics;
using WardogsXpTracker.Core.Persistence;
using WardogsXpTracker.Core.Tracking;

namespace WardogsXpTracker.App;

public sealed record MockScreenshot(string File, string Name, string Instructions, bool Snapshot);

public partial class MainWindow
{
    private const string MockResourcePrefix = "WardogsXpTracker.MockData.";
    private readonly HashSet<Guid> mockSeasonIds = [];
    private bool mockScreenshotMode;
    private SessionTracker? realTrackerBeforeMocks;
    private bool realResumeBeforeMocks;
    private SeasonDocument? fakeAnalyticsSeason;
    private SessionTracker? fakeAnalyticsTracker;
    private SeasonAnalytics? latestAnalytics;
    private MarkdownSaveStore? mockStore;
    private bool mockPackReady;
    private MarkdownSaveStore MockStore => mockStore ??= new(Path.Combine(saveStore?.RootDirectory ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MadLicorice", "WardogsXpTracker"), "mock-data", "0.3.7"));
    private bool CompletionSoundEnabled => mockScreenshotMode ? TestToolsEnabled && MockSoundToggle.IsChecked == true : preferences.CompletionSound;

    private void PlayCompletionCue()
    {
        if (!CompletionAudio.TryPlay(out var error)) SessionNotice.Text = $"Completion reached. Sound could not play: {error}";
    }

    private void PreviewSound_Click(object sender, RoutedEventArgs e)
    {
        if (!TestToolsEnabled || busy || isClosing) return;
        TestStatus.Text = CompletionAudio.TryPlay(out var error) ? "Played the completion sound once. This preview does not change your sound preference."
            : $"Sound could not play: {error}";
    }

    private void UpdateTestControls(bool available)
    {
        var enabled = TestToolsEnabled;
        UpdateTestToolsPreferencePresentation(available);
        TestToolsTab.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        TestToolsTab.IsEnabled = enabled && available;
        LoadFakeStatsButton.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        if (!enabled && TestToolsTab.IsSelected) MainTabs.SelectedItem = SessionTrackerTab;
        LoadMocksButton.IsEnabled = enabled && available;
        LoadFakeStatsButton.IsEnabled = enabled && available;
        LoadBaselineCoverageDemoButton.IsEnabled = enabled && available;
        ReturnRealDataButton.IsEnabled = enabled && !busy && (mockScreenshotMode || fakeAnalyticsSeason is not null);
        ReadMockButton.IsEnabled = enabled && available && mockScreenshotMode;
        NextMockButton.IsEnabled = enabled && available && mockScreenshotMode;
        HeaderNextMockButton.IsEnabled = enabled && available && mockScreenshotMode;
        HeaderNextMockButton.Visibility = enabled && mockScreenshotMode ? Visibility.Visible : Visibility.Collapsed;
        MockSelector.IsEnabled = enabled && available && mockScreenshotMode;
        MockModeBanner.Visibility = enabled && (mockScreenshotMode || fakeAnalyticsSeason is not null) ? Visibility.Visible : Visibility.Collapsed;
        WindowSelector.IsEnabled = !busy && !isClosing;
        OptionsMockNotice.Visibility = mockScreenshotMode ? Visibility.Visible : Visibility.Collapsed;
        AutoReadToggle.IsEnabled = !mockScreenshotMode && available;
        MockStepPanel.Visibility = enabled && mockScreenshotMode ? Visibility.Visible : Visibility.Collapsed;
        MockFolderText.Text = mockStore?.RootDirectory ?? "The mock folder appears after the first test load.";
        if (!enabled && TestStatus.Text.StartsWith("Choose Load fake screenshots", StringComparison.Ordinal)) TestStatus.Text = "";
        RefreshAnalyticsSourcePresentation();
    }

    private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReferenceEquals(e.Source, MainTabs))
        {
            UpdatePageScrollMode();
            RefreshAnalyticsIfVisible();
            RefreshSavedResults();
        }
    }

    private void RefreshAnalyticsIfVisible()
    {
        if (AnalyticsTab?.IsSelected != true || AnalyticsSourceSeason is not { } document) return;
        try
        {
            latestAnalytics = importedAnalytics?.Report ?? AnalyticsEngine.Build(document);
            AnalyticsDisplay.SetReport(latestAnalytics, AnalyticsSourceIsMock);
            RefreshAnalyticsSourcePresentation();
        }
        catch (Exception exception)
        { AnalyticsTransferStatus.Text = $"Analytics could not be calculated: {exception.Message}"; }
    }

    private async Task EnsureMockPackAsync()
    {
        if (!TestToolsEnabled) throw new InvalidOperationException("Enable Test Tools in Options before loading mock data.");
        if (mockPackReady) return;
        var directory = MockStore.RootDirectory;
        var assembly = Assembly.GetExecutingAssembly();
        Directory.CreateDirectory(directory);
        foreach (var resource in assembly.GetManifestResourceNames().Where(n => n.StartsWith(MockResourcePrefix, StringComparison.Ordinal)))
        {
            var filename = resource[MockResourcePrefix.Length..];
            if (Path.GetFileName(filename) != filename) throw new InvalidDataException("The bundled mock filename is invalid.");
            var destination = Path.Combine(directory, filename);
            if (File.Exists(destination)) continue;
            using var source = assembly.GetManifestResourceStream(resource) ?? throw new InvalidDataException("A bundled mock file is missing.");
            using var target = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await source.CopyToAsync(target);
        }
        if (!File.Exists(Path.Combine(directory, "completion-preview.wav")))
            await File.WriteAllBytesAsync(Path.Combine(directory, "completion-preview.wav"), CompletionAudio.CopyWave());
        using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory, "manifest.json")));
        var cases = manifest.RootElement.GetProperty("Cases").EnumerateArray().Select(item =>
        {
            var file = item.GetProperty("File").GetString() ?? throw new InvalidDataException("A mock file has no name.");
            if (Path.GetFileName(file) != file || !file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The mock manifest contains an unsupported filename.");
            var action = item.TryGetProperty("Action", out var actionText) ? actionText.GetString() ?? "" : "";
            return new MockScreenshot(file, FriendlyMockName(file), FriendlyMockAction(file, action),
                file.Contains("baseline", StringComparison.OrdinalIgnoreCase));
        }).ToArray();
        if (cases.Length is < 1 or > 15) throw new InvalidDataException("The mock pack must contain between one and fifteen images.");
        MockSelector.ItemsSource = cases;
        MockSelector.SelectedIndex = 0;
        MockFolderText.Text = directory;
        mockPackReady = true;
    }

    private async void LoadMocks_Click(object sender, RoutedEventArgs e)
    {
        if (!TestToolsEnabled || !CanChangeTracker()) return;
        var loaded = false;
        SetBusy(true);
        try
        {
            PauseAutomaticReading();
            rebaseWindow?.Close();
            if (!mockScreenshotMode)
            {
                await PersistTrackerAsync();
                if (persistenceBlocked) return;
                realTrackerBeforeMocks = tracker;
                realResumeBeforeMocks = resumePending;
            }
            else await PersistTrackerAsync();
            if (persistenceBlocked) return;
            await EnsureMockPackAsync();
            if (fakeAnalyticsSeason is not null) await MockStore.SaveAsync(fakeResultsFilename, fakeAnalyticsSeason);
            fakeAnalyticsSeason = null;
            fakeAnalyticsTracker = null;
            TrackerReadFeedback.Visibility = Visibility.Collapsed;
            tracker = new(new SeasonDocument { Name = "MOCK — Screenshot walkthrough" });
            mockSeasonIds.Add(tracker.Document.SeasonId);
            mockScreenshotMode = true;
            resumePending = false;
            tracker.StartSession(rules: selectedRules);
            foreach (var candidate in Enum.GetValues<WardogsXpTracker.Core.Reading.XpClass>())
                tracker.SetTrackEnabled(candidate, candidate == WardogsXpTracker.Core.Reading.XpClass.Assault);
            MockSelector.SelectedIndex = 0;
            TestStatus.Text = "MOCK DATA: walkthrough ready. Read the baseline, then use Next mock for each tier. No real season values change.";
            SessionNotice.Text = "MOCK DATA. Read the first image to establish the Assault baseline.";
            await PersistTrackerAsync();
            if (persistenceBlocked) return;
            ClearImportedAnalytics();
            RefreshTracker();
            MainTabs.SelectedItem = TestToolsTab;
            loaded = true;
        }
        catch (Exception exception) { TestStatus.Text = $"Mock screenshots could not load: {exception.Message}"; }
        finally { FinishWork(); }
        if (loaded)
        {
            MainTabs.SelectedItem = SessionTrackerTab;
            await ReadMockScreenshotAsync(true);
        }
    }

    private async void ReadMock_Click(object sender, RoutedEventArgs e)
    {
        if (!TestToolsEnabled) return;
        if (MockSelector.SelectedItem is MockScreenshot selected) await ReadMockScreenshotAsync(selected.Snapshot);
    }

    private async void NextMock_Click(object sender, RoutedEventArgs e)
    {
        if (!TestToolsEnabled || busy || !mockScreenshotMode) return;
        if (MockSelector.SelectedIndex + 1 >= MockSelector.Items.Count)
        { TestStatus.Text = "Last image reached. Confirm final results on Session Tracker, or load fake stats on Analytics. Reload screenshots to start again."; return; }
        MockSelector.SelectedIndex++;
        if (MockSelector.SelectedItem is MockScreenshot selected) await ReadMockScreenshotAsync(selected.Snapshot);
    }

    private async Task ReadMockScreenshotAsync(bool asSnapshot)
    {
        if (!TestToolsEnabled || busy || !mockScreenshotMode || !CanChangeTracker() || MockSelector.SelectedItem is not MockScreenshot selected) return;
        SetBusy(true);
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(MockResourcePrefix + selected.File)
                ?? throw new InvalidDataException("The bundled mock image is missing.");
            var bitmap = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            bitmap.Freeze();
            currentSource = $"MOCK · {selected.File} · {bitmap.PixelWidth} × {bitmap.PixelHeight}";
            await ReadImageAsync(bitmap, new("mock", "Bundled mock PNG through local OCR", currentSource, DateTimeOffset.UtcNow, null),
                GetTrackerCaptureContext(asSnapshot, asSnapshot ? ReadingSource.SnapshotHotkey : ReadingSource.CheckHotkey));
            MockImagePreview.Source = bitmap;
            MockStepTitle.Text = $"MOCK {MockSelector.SelectedIndex + 1}/{MockSelector.Items.Count} · {selected.Name}";
            MockStepMessage.Text = selected.Instructions;
            TestStatus.Text = $"MOCK {MockSelector.SelectedIndex + 1}/{MockSelector.Items.Count}: {selected.Instructions} {SessionNotice.Text}";
        }
        catch (Exception exception) { TestStatus.Text = $"Mock read failed: {exception.Message}"; }
        finally { FinishWork(); }
    }

    private async void LoadFakeStats_Click(object sender, RoutedEventArgs e)
    {
        if (!TestToolsEnabled || !CanChangeTracker()) return;
        SetBusy(true);
        try
        {
            await EnsureMockPackAsync();
            if (fakeAnalyticsSeason is not null) await MockStore.SaveAsync(fakeResultsFilename, fakeAnalyticsSeason);
            var loaded = await MockStore.LoadAsync<SeasonDocument>("demo-analytics-season.md") ?? DemoStatsFactory.CreateAnalyticsSeason();
            var loadedTracker = new SessionTracker(loaded);
            await MockStore.SaveAsync("demo-analytics-season.md", loaded);
            fakeAnalyticsSeason = loaded;
            fakeAnalyticsTracker = loadedTracker;
            fakeResultsFilename = "demo-analytics-season.md";
            ClearImportedAnalytics();
            RefreshSavedResults();
            MainTabs.SelectedItem = AnalyticsTab;
            RefreshAnalyticsIfVisible();
            TestStatus.Text = "Fake analytics loaded. This dataset is separate from every real season.";
        }
        catch (Exception exception) { TestStatus.Text = $"Fake stats could not load: {exception.Message}"; }
        finally { FinishWork(); }
    }

    private async void ReturnRealData_Click(object sender, RoutedEventArgs e)
    {
        if (!TestToolsEnabled || busy || isClosing) return;
        SetBusy(true);
        try
        {
            if (await RestoreRealDataAsync(allowDiscardPrompt: true)) ClearImportedAnalytics();
        }
        finally { FinishWork(); }
    }

    // The caller reserves the busy state before awaiting this method.
    // Test-tools settings can call it without clearing an imported analytics selection.
    private async Task<bool> RestoreRealDataAsync(bool allowDiscardPrompt)
    {
        if (HasPendingSavedResultDeletionSave && !await RetrySavedResultDeletionAsync()) return false;
        rebaseWindow?.Close();
        if (mockScreenshotMode)
        {
            if (realTrackerBeforeMocks is null)
            { SessionNotice.Text = "The real session reference is unavailable. Keep the mock session open to preserve its data."; return false; }
            await PersistTrackerAsync();
            if (persistenceBlocked)
            {
                if (!allowDiscardPrompt)
                { SessionNotice.Text = "Test Tools stay enabled because mock changes could not save. Use Retry save before disabling them."; return false; }
                if (TrackerDialogs.Choose(this, "Unsaved mock changes",
                    "The last mock changes could not be saved. Stay in mock mode to use Retry save, or return to real data without those test changes. Real season files remain unchanged.",
                    "Stay in mock mode", "Return without unsaved mock changes") != 1) return false;
            }
        }
        if (fakeAnalyticsSeason is not null)
        {
            try { await MockStore.SaveAsync(fakeResultsFilename, fakeAnalyticsSeason); }
            catch (Exception exception)
            { SessionNotice.Text = $"Fake analytics could not save: {exception.Message} The test data stays available. Retry before leaving it."; return false; }
        }
        if (mockScreenshotMode)
        {
            tracker = realTrackerBeforeMocks;
            resumePending = realResumeBeforeMocks;
            mockScreenshotMode = false;
            realTrackerBeforeMocks = null;
            persistenceBlocked = false;
            TrackerReadFeedback.Visibility = Visibility.Collapsed;
        }
        fakeAnalyticsSeason = null;
        fakeAnalyticsTracker = null;
        SessionNotice.Text = "Returned to real season data. Select New Session if the actual match changed during testing.";
        SaveStatus.Text = "Real season restored. Mock records remain in their separate folder.";
        TestStatus.Text = "Test mode ended. Real season values and preferences were preserved.";
        RestoreAutomaticReadPreference();
        RefreshTracker();
        RefreshAnalyticsIfVisible();
        return true;
    }

    private async void ExportAnalytics_Click(object sender, RoutedEventArgs e)
    {
        if (busy || latestAnalytics is null) return;
        SetBusy(true);
        try
        {
            RefreshAnalyticsIfVisible();
            var dialog = new SaveFileDialog { Title = "Export analytics snapshot", Filter = "Markdown|*.md", DefaultExt = ".md", FileName = "wardogs-analytics.md" };
            if (dialog.ShowDialog(this) != true) return;
            var directory = Path.GetDirectoryName(Path.GetFullPath(dialog.FileName))!;
            var filename = Path.GetFileName(dialog.FileName);
            if (string.Equals(directory, saveStore?.RootDirectory, StringComparison.OrdinalIgnoreCase)
                && (filename.Equals("settings.md", StringComparison.OrdinalIgnoreCase)
                    || filename.StartsWith("season-", StringComparison.OrdinalIgnoreCase)
                    || filename.StartsWith("rules-", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Choose a report filename that does not replace an app save or rules file.");
            var source = AnalyticsSourceSeason ?? throw new InvalidOperationException("No analytics source is selected.");
            await new MarkdownSaveStore(directory).SaveAsync(filename, new AnalyticsExport(latestAnalytics!, source,
                AnalyticsSourceIsMock));
            AnalyticsTransferStatus.Text = "Analytics snapshot exported with its calculation version. Received and missed XP remain separate.";
        }
        catch (Exception exception) { AnalyticsTransferStatus.Text = $"Analytics export failed: {exception.Message}"; }
        finally { FinishWork(); }
    }

    private static string FriendlyMockName(string file)
    {
        var step = int.TryParse(file.AsSpan(0, 2), out var value) ? value : 0;
        return step switch
        {
            1 => "01 · Starting XP — Assault 56,486",
            2 => "02 · First earned XP — +1",
            3 => "03 · Tier 1 — 500 earned XP",
            4 => "04 · Tier 2 — 1,000 earned XP",
            5 => "05 · Tier 3 — 1,500 earned XP",
            6 => "06 · Tier 4 — 2,000 earned XP",
            7 => "07 · Just below target — 2,499 earned XP",
            8 => "08 · Maximum tier — 2,500 earned XP + sound",
            9 => "09 · Beyond target — 3,000 earned XP",
            10 => "10 · Completed rewards page — read must be rejected",
            11 => "11 · End score — DEFEAT, 2nd or 3rd place",
            12 => "12 · Final XP — 6,500 total gain INCLUDING bonus",
            _ => Path.GetFileNameWithoutExtension(file).Replace('-', ' ')
        };
    }

    private static string FriendlyMockAction(string file, string fallback) => file[..2] switch
    {
        "09" => "Assault current XP is 59,486. The 3,000 earned XP keeps counting beyond the target.",
        "10" => "Rejection test: only completed rewards are visible. Keep the previous 59,486 current XP and green check. The rejection banner explains why.",
        "11" => "Placement only: DEFEAT means 2nd or 3rd place. Live checks freeze. Read the next mock to capture final XP before recording results.",
        "12" => "Final Assault cumulative XP is 62,986. Starting 56,486 + 6,500 total gain, including bonus. Record Final XP to save and view the summary. Unconfirmed baseline timing keeps sources unknown.",
        _ => fallback
    };

    private sealed record AnalyticsExport(SeasonAnalytics Report, SeasonDocument SourceSeason, bool IsMockData);
}
