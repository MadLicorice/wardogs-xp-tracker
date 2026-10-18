using System.Windows;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using WardogsXpTracker.Core.Reading;
using WardogsXpTracker.Core.Tracking;
using WardogsXpTracker.Windows.Capture;

namespace WardogsXpTracker.App;

public static class AutomaticReadPolicy
{
    public static bool NeedsMatchRead(TrackerSession? session) => session is not null
        && session.ConfirmedFinal is null
        && (session.State == TrackerSessionState.Active || session.State == TrackerSessionState.Frozen);

    public static bool IsRelevantScene(ScreenReadResult result) => result.Screen == ScreenKind.Unlock || result.IsEndScreen;
}

public sealed record CaptureModeChoice(CaptureMode Mode, string Label);

public partial class MainWindow
{
    private bool refreshingCaptureOptions;

    [DllImport("user32.dll", EntryPoint = "GetForegroundWindow")]
    private static extern nint ForegroundCaptureWindow();

    private bool AutomaticTargetHasFocus => SelectedCaptureMode != CaptureMode.QuietVisibleClient
        || WindowSelector.SelectedItem is CaptureTarget target && ForegroundCaptureWindow() == target.WindowHandle;

    private bool CanAutomaticallyReadNow => trackerReady && !closed && !isClosing && !persistenceBlocked
        && !resumePending && !switchingSeason && !mockScreenshotMode
        && AutoReadToggle.IsChecked == true && WindowSelector.SelectedItem is CaptureTarget
        && AutomaticReadPolicy.NeedsMatchRead(tracker?.ActiveSession);

    private CaptureMode SelectedCaptureMode => Enum.TryParse<CaptureMode>(preferences.CaptureMode, out var mode)
        && Enum.IsDefined(mode) ? mode : CaptureMode.QuietVisibleClient;

    private void InitializeCaptureOptions()
    {
        refreshingCaptureOptions = true;
        try
        {
            CaptureModeSelector.ItemsSource = new[]
            {
                new CaptureModeChoice(CaptureMode.QuietVisibleClient, "Quiet · visible game only"),
                new CaptureModeChoice(CaptureMode.WindowGraphicsCapture, "Window capture · Windows border")
            };
            CaptureModeSelector.SelectedValue = SelectedCaptureMode;
            AutoReadToggle.IsChecked = preferences.AutomaticReadEnabled;
        }
        finally { refreshingCaptureOptions = false; }
        SyncAutomaticReading();
    }

    private void RestoreAutomaticReadPreference()
    {
        refreshingCaptureOptions = true;
        try { AutoReadToggle.IsChecked = preferences.AutomaticReadEnabled; }
        finally { refreshingCaptureOptions = false; }
        SyncAutomaticReading();
    }

    private void PauseAutomaticReading()
    {
        refreshingCaptureOptions = true;
        try { AutoReadToggle.IsChecked = false; }
        finally { refreshingCaptureOptions = false; }
        automaticTimer.Stop();
    }

    private void SyncAutomaticReading()
    {
        if (CanAutomaticallyReadNow)
        {
            if (!automaticTimer.IsEnabled) automaticTimer.Start();
        }
        else
        {
            automaticTimer.Stop();
            if (!busy) _ = StopCaptureAsync();
        }
    }

    private async Task StopCaptureAsync()
    {
        try { await capture.StopAsync(); }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    private void GameWindow_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        if (!trackerReady || refreshingCaptureOptions || busy) return;
        RestoreAutomaticReadPreference();
    }

    private async void AutoRead_Changed(object sender, RoutedEventArgs args)
    {
        if (refreshingCaptureOptions || !trackerReady || mockScreenshotMode || busy) return;
        preferences.AutomaticReadEnabled = AutoReadToggle.IsChecked == true;
        SetBusy(true);
        try { await PersistPreferencesAsync(); }
        finally { FinishWork(); }
    }

    private async void CaptureMode_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        if (refreshingCaptureOptions || !CanChangeTracker() || CaptureModeSelector.SelectedItem is not CaptureModeChoice choice) return;
        SetBusy(true);
        try
        {
            await capture.StopAsync();
            preferences.CaptureMode = choice.Mode.ToString();
            await PersistPreferencesAsync();
        }
        finally { FinishWork(); }
    }
}
