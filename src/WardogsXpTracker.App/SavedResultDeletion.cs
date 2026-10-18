using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using WardogsXpTracker.Core.Persistence;
using WardogsXpTracker.Core.Tracking;

namespace WardogsXpTracker.App;

public partial class MainWindow
{
    private sealed record SavedResultDeletionSave(SessionTracker Owner, MarkdownSaveStore Store,
        string Filename, Guid SessionId, bool IsMock);

    private SavedResultDeletionSave? pendingSavedResultDeletion;
    private bool HasPendingSavedResultDeletionSave => pendingSavedResultDeletion is not null;

    private void SavedResultMenu_Click(object sender, RoutedEventArgs args)
    {
        if (sender is Button button) OpenSavedResultMenu(button);
        args.Handled = true;
    }

    private void SavedResultMenu_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs args)
    {
        if (sender is Button button) OpenSavedResultMenu(button);
        // The row menu must not select its DataGrid row as a side effect.
        args.Handled = true;
    }

    private void OpenSavedResultMenu(Button button)
    {
        if (busy || isClosing || switchingSeason || persistenceBlocked || !trackerReady
            || button.ContextMenu is not { } menu) return;
        menu.PlacementTarget = button;
        menu.Placement = PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private async void DeleteSavedResult_Click(object sender, RoutedEventArgs args)
    {
        if (busy || !trackerReady || isClosing || switchingSeason
            || sender is not FrameworkElement { Tag: Guid sessionId } || ResultsOwner is not { } owner) return;
        if (HasPendingSavedResultDeletionSave || persistenceBlocked)
        {
            SavedResultsStatus.Text = "Resolve the save error with Retry save before deleting another match.";
            return;
        }
        var session = owner.Document.Sessions.FirstOrDefault(item => item.Id == sessionId);
        if (session is null) { RefreshSavedResults(); return; }
        // ResultsOwner only exposes writable real/mock data. An imported report is never an owner.
        var isFakeResults = ReferenceEquals(owner, fakeAnalyticsTracker);
        var isMockSession = mockSeasonIds.Contains(owner.Document.SeasonId);
        var destination = isFakeResults || isMockSession ? MockStore : saveStore;
        if (destination is null)
        {
            SavedResultsStatus.Text = "The local save is unavailable. This match was not deleted.";
            return;
        }
        var filename = isFakeResults ? fakeResultsFilename
            : isMockSession ? $"mock-session-{owner.Document.SeasonId:N}.md" : SeasonFile(owner.Document.SeasonId);
        SetBusy(true);
        try
        {
            if (!ConfirmSavedResultDeletion()) return;
            if (!ReferenceEquals(ResultsOwner, owner)
                || !owner.Document.Sessions.Any(item => ReferenceEquals(item, session)))
            {
                SavedResultsStatus.Text = "The selected results changed. This match was not deleted.";
                RefreshSavedResults();
                return;
            }
            var deletesCurrent = ReferenceEquals(owner, tracker) && owner.Document.ActiveSessionId == sessionId;
            var result = owner.DeleteSession(sessionId);
            if (!result.Applied || !result.Changed)
            {
                SavedResultsStatus.Text = result.Message;
                RefreshSavedResults();
                return;
            }
            if (deletesCurrent)
            {
                resumePending = false;
                rebaseWindow?.Close();
                SetReadFeedback(true, "");
                SessionNotice.Text = "Current match deleted. Select New Session for a new match.";
            }
            pendingSavedResultDeletion = new(owner, destination, filename, sessionId, isFakeResults || isMockSession);
            RefreshTracker();
            RefreshSavedResults();
            RefreshAnalyticsIfVisible();
            RefreshInteractivePresentation();
            await RetrySavedResultDeletionAsync();
        }
        finally { FinishWork(); }
    }

    private bool ConfirmSavedResultDeletion()
    {
        var panel = new StackPanel { Margin = new Thickness(22) };
        panel.Children.Add(new TextBlock
        {
            Text = "This action cannot be undone. Delete?",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16)
        });
        var dialog = new Window
        {
            Owner = this, Title = "Delete saved match?", Width = Math.Min(470, SystemParameters.WorkArea.Width - 24),
            SizeToContent = SizeToContent.Height, ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = panel,
            Style = (Style)FindResource(typeof(Window))
        };
        var yes = new Button { Content = "Yes", Margin = new Thickness(0, 0, 0, 8) };
        var no = new Button { Content = "No", IsDefault = true, IsCancel = true };
        yes.Click += (_, _) => dialog.DialogResult = true;
        no.Click += (_, _) => dialog.DialogResult = false;
        panel.Children.Add(yes);
        panel.Children.Add(no);
        dialog.Loaded += (_, _) => no.Focus();
        return dialog.ShowDialog() == true;
    }

    // The caller reserves busy. Retry writes the retained owner to its original destination.
    // It never repeats DeleteSession and never substitutes the currently selected dataset.
    private async Task<bool> RetrySavedResultDeletionAsync()
    {
        if (pendingSavedResultDeletion is not { } pending) return true;
        try
        {
            await pending.Store.SaveAsync(pending.Filename, pending.Owner.Document);
            pendingSavedResultDeletion = null;
            persistenceBlocked = false;
            SavedResultsStatus.Text = pending.IsMock
                ? "Match deleted and saved in its separate mock dataset. Real data is unchanged."
                : "Match deleted and saved. Its XP and analytics records were removed.";
            SaveStatus.Text = pending.IsMock ? "Mock match deletion saved separately." : $"Match deletion saved locally · {DateTime.Now:T}";
            RefreshTracker();
            RefreshSavedResults();
            RefreshAnalyticsIfVisible();
            RefreshInteractivePresentation();
            return true;
        }
        catch (Exception exception)
        {
            persistenceBlocked = true;
            PauseAutomaticReading();
            SavedResultsStatus.Text = "The match is removed in memory, but deletion could not save. Keep the app open and use Retry save.";
            SaveStatus.Text = $"Match deletion save failed: {exception.Message} Keep the app open. Use Retry save.";
            SetTrackerBusy(busy);
            return false;
        }
    }
}
