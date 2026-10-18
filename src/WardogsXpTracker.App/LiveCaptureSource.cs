using System.Windows;

namespace WardogsXpTracker.App;

public partial class MainWindow
{
    private async Task<bool> ActivateGameSourceAsync()
    {
        if (busy || closed || isClosing) return false;
        if (!mockScreenshotMode) return true;
        SetBusy(true);
        try
        {
            if (!await RestoreRealDataAsync(allowDiscardPrompt: false)) return false;
            SessionNotice.Text = "Game capture restored. Select the WARDOGS window. Mock readings remain saved separately.";
            return true;
        }
        finally { FinishWork(); }
    }

    private async void WindowSelector_DropDownOpened(object sender, EventArgs e)
    {
        if (!mockScreenshotMode) return;
        WindowSelector.IsDropDownOpen = false;
        if (!await ActivateGameSourceAsync()) return;
        RefreshTargets();
        if (!closed && !busy) WindowSelector.IsDropDownOpen = true;
    }
}
