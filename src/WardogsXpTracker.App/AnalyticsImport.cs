using System.Windows;
using Microsoft.Win32;
using WardogsXpTracker.Core.Tracking;

namespace WardogsXpTracker.App;

public partial class MainWindow
{
    private AnalyticsImportData? importedAnalytics;
    private SeasonDocument? AnalyticsSourceSeason => importedAnalytics?.SourceSeason ?? fakeAnalyticsSeason ?? tracker?.Document;
    private bool AnalyticsSourceIsMock => importedAnalytics?.IsMockData ?? (fakeAnalyticsSeason is not null || mockScreenshotMode);

    private void RefreshAnalyticsSourcePresentation()
    {
        ImportAnalyticsButton.IsEnabled = trackerReady && !busy && !isClosing;
        ReturnCurrentAnalyticsButton.Visibility = importedAnalytics is null ? Visibility.Collapsed : Visibility.Visible;
        ReturnCurrentAnalyticsButton.IsEnabled = !busy && !isClosing;
        AnalyticsSourceStatus.Text = importedAnalytics is { } imported
            ? $"IMPORTED · {imported.FileName} · {imported.SourceSeason.Name} · Analytics only; current session unchanged"
            : AnalyticsSourceIsMock ? $"MOCK DATA · {AnalyticsSourceSeason?.Name ?? "No dataset"}"
                : $"CURRENT SEASON · {AnalyticsSourceSeason?.Name ?? "Loading local data…"}";
        AnalyticsSourceStatus.ToolTip = importedAnalytics?.SourcePath;
    }

    private void ClearImportedAnalytics()
    {
        importedAnalytics = null;
        RefreshAnalyticsSourcePresentation();
    }

    private async void ImportAnalytics_Click(object sender, RoutedEventArgs e)
    {
        if (busy || closed || isClosing || !trackerReady) return;
        SetBusy(true);
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import analytics or season records",
                Filter = "Analytics exports and season saves|*.md",
                CheckFileExists = true,
                Multiselect = false
            };
            if (dialog.ShowDialog(this) != true) return;
            await LoadImportedAnalyticsAsync(dialog.FileName);
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception exception)
        {
            AnalyticsTransferStatus.Text = $"Analytics import failed: {exception.Message} The current dataset and session are unchanged.";
        }
        finally { FinishWork(); }
    }

    private async Task<bool> LoadImportedAnalyticsAsync(string? path)
    {
        var loaded = await AnalyticsImportReader.LoadAsync(path, lifetime.Token);
        if (loaded is null || closed) return false;
        importedAnalytics = loaded;
        MainTabs.SelectedItem = AnalyticsTab;
        RefreshAnalyticsIfVisible();
        RefreshAnalyticsSourcePresentation();
        AnalyticsTransferStatus.Text = $"Imported {loaded.FileName} for analytics only. The current session and local save files are unchanged.";
        return true;
    }

    private void ReturnCurrentAnalytics_Click(object sender, RoutedEventArgs e)
    {
        if (busy || closed || isClosing) return;
        ClearImportedAnalytics();
        RefreshAnalyticsIfVisible();
        AnalyticsTransferStatus.Text = "Returned to current analytics. The session and local save files are unchanged.";
    }
}
