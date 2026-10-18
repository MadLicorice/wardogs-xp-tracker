using System.Windows;
using System.Windows.Threading;

namespace WardogsXpTracker.App;

public partial class MainWindow
{
    private bool updatingTestToolsToggle;
    private DispatcherTimer? testToolsFlashTimer;
    private object? testToolsPreviousTag;
    private bool testToolsFlashActive;
    private bool TestToolsEnabled => trackerReady && preferences.EnableTestTools;

    private void UpdateTestToolsPreferencePresentation(bool available)
    {
        updatingTestToolsToggle = true;
        try
        {
            EnableTestToolsToggle.IsChecked = preferences.EnableTestTools;
            EnableTestToolsToggle.IsEnabled = available && !isClosing;
        }
        finally { updatingTestToolsToggle = false; }
        if (!TestToolsEnabled) StopTestToolsFlash();
    }

    private async void TestToolsEnabled_Changed(object sender, RoutedEventArgs e)
    {
        if (updatingTestToolsToggle) return;
        if (refreshingTracker || !trackerReady || busy || switchingSeason || persistenceBlocked || isClosing)
        { UpdateTestToolsPreferencePresentation(false); return; }
        var enabled = EnableTestToolsToggle.IsChecked == true;
        var previous = preferences.EnableTestTools;
        if (enabled == previous) return;
        var committed = false;
        SetBusy(true);
        try
        {
            if (!enabled && !await RestoreRealDataAsync(allowDiscardPrompt: false)) return;
            preferences.EnableTestTools = enabled;
            if (!await PersistPreferencesAsync())
            { preferences.EnableTestTools = previous; return; }
            committed = true;
            UpdateTestControls(false);
            RefreshAnalyticsIfVisible();
        }
        catch (Exception exception)
        {
            preferences.EnableTestTools = previous;
            SessionNotice.Text = $"Test Tools could not change: {exception.Message} Existing session data stays available.";
        }
        finally
        {
            FinishWork();
            if (committed && enabled) FlashTestToolsTab();
        }
    }

    private void FlashTestToolsTab()
    {
        StopTestToolsFlash();
        if (!TestToolsEnabled || closed) return;
        testToolsPreviousTag = TestToolsTab.ReadLocalValue(FrameworkElement.TagProperty);
        testToolsFlashActive = true;
        TestToolsTab.Tag = "JustEnabled";
        testToolsFlashTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        testToolsFlashTimer.Tick += (_, _) => StopTestToolsFlash();
        testToolsFlashTimer.Start();
    }

    private void StopTestToolsFlash()
    {
        testToolsFlashTimer?.Stop();
        testToolsFlashTimer = null;
        if (!testToolsFlashActive) return;
        if (testToolsPreviousTag == DependencyProperty.UnsetValue) TestToolsTab.ClearValue(FrameworkElement.TagProperty);
        else TestToolsTab.SetValue(FrameworkElement.TagProperty, testToolsPreviousTag);
        testToolsFlashActive = false;
        testToolsPreviousTag = null;
    }
}
