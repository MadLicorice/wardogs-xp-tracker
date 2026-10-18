using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using WardogsXpTracker.Core.Reading;
using WardogsXpTracker.Core.Tracking;

namespace WardogsXpTracker.App;

public sealed record SavedResultRow(Guid SessionId, DateTimeOffset StartedAt, string Status,
    string ObservedPlacement, string FinalXpText, string ReviewLabel)
{
    public string StartedText => StartedAt.ToLocalTime().ToString("g");
}

public partial class MainWindow
{
    private SessionTracker? ResultsOwner => fakeAnalyticsTracker ?? tracker;

    private void SetReadFeedback(bool accepted, string message)
    {
        TrackerReadFeedback.Visibility = accepted ? Visibility.Collapsed : Visibility.Visible;
        TrackerReadFeedbackText.Text = message;
    }

    private void RefreshFinalPrompts()
    {
        var pending = tracker?.ActiveSession?.PendingFinal;
        PendingFinalPanel.Visibility = pending is { KeepOldReferencesChosen: false } ? Visibility.Visible : Visibility.Collapsed;
        RecordFinalButton.Content = pending?.Tracks.Count > 0 ? "Record Final XP" : "Waiting for final XP";
        RecordFinalButton.ToolTip = pending?.Tracks.Count > 0 ? "Confirm the captured final XP and view the match summary."
            : $"No final XP was captured. Open the match-end PROGRESSION tab and push {checkBinding}.";
        var hasTotals = pending?.Tracks.Count > 0;
        PendingFinalTitle.Text = hasTotals ? "Final XP captured · Ready to record"
            : "End detected · Final XP not captured";
        PendingFinalMessage.Text = hasTotals
            ? "Observed results stay saved. Record them to view the XP summary, or review them later in Saved Results."
            : mockScreenshotMode ? "This score image contains no final XP. Use Next mock + read to capture mock 12, then record results."
                : $"No final XP captured yet. Open the match-end PROGRESSION tab, then push {checkBinding}.";
        var deferred = tracker?.Document.Sessions.Count(s => s.PendingFinal?.KeepOldReferencesChosen == true) ?? 0;
        DeferredFinalPanel.Visibility = deferred > 0 && PendingFinalPanel.Visibility != Visibility.Visible
            ? Visibility.Visible : Visibility.Collapsed;
        DeferredFinalText.Text = $"{deferred} match result(s) saved for later. Review them in Saved Results. Observed values stay saved.";
        DefaultSessionPanel.Visibility = PendingFinalPanel.Visibility == Visibility.Visible || deferred > 0
            ? Visibility.Collapsed : Visibility.Visible;
    }

    private void RefreshSavedResults()
    {
        if (SavedResultsGrid is null || ResultsOwner is not { } owner) return;
        var pendingCount = owner.Document.Sessions.Count(s => s.PendingFinal is not null);
        SavedResultsTab.Header = pendingCount == 0 ? "Saved Results" : $"Saved Results ({pendingCount})";
        SavedResultsScope.Text = $"{owner.Document.Name} · {owner.Document.Sessions.Count} matches · {pendingCount} awaiting review"
            + (fakeAnalyticsTracker is not null || mockScreenshotMode ? " · MOCK DATA" : "");
        var grid = SavedResultsGrid;
        var sort = grid.Items.SortDescriptions.ToArray();
        var directions = grid.Columns.Select(c => c.SortDirection).ToArray();
        var selectedId = (grid.SelectedItem as SavedResultRow)?.SessionId;
        var rows = owner.Document.Sessions.Select(s => new SavedResultRow(s.Id, s.StartedAt,
            s.PendingFinal is { Tracks.Count: 0 } ? "Waiting for final XP"
                : s.PendingFinal is { KeepOldReferencesChosen: true } ? "Saved for later"
                : s.PendingFinal is not null ? "Needs confirmation"
                : s.ConfirmedFinal is not null ? "Confirmed"
                : s.Outcome is SessionOutcome.LeftMatch or SessionOutcome.Disconnected ? "No bonus reported"
                : "No final XP recorded",
            ObservedPlacementText(s), FinalTotalsText(s.PendingFinal ?? s.ConfirmedFinal),
            s.PendingFinal is not null ? "Review…" : s.ConfirmedFinal is not null ? "Summary…" : "Details…")).ToArray();
        grid.ItemsSource = rows;
        grid.Items.SortDescriptions.Clear();
        if (sort.Length == 0)
        {
            sort = [new SortDescription(nameof(SavedResultRow.StartedAt), ListSortDirection.Descending)];
            var dateColumn = grid.Columns.FirstOrDefault(column => column.SortMemberPath == nameof(SavedResultRow.StartedAt));
            if (dateColumn is not null) directions[grid.Columns.IndexOf(dateColumn)] = ListSortDirection.Descending;
        }
        foreach (var description in sort) grid.Items.SortDescriptions.Add(description);
        for (var i = 0; i < directions.Length; i++) grid.Columns[i].SortDirection = directions[i];
        grid.SelectedItem = rows.FirstOrDefault(r => r.SessionId == selectedId);
    }

    private static string FinalTotalsText(FinalResultSet? set) => set?.Tracks.Count > 0
        ? string.Join(" · ", set.Tracks.OrderBy(p => p.Key).Select(p => $"{p.Key} {p.Value.Xp:N0}"))
        : "No final XP observed";

    private static string ObservedPlacementText(TrackerSession session)
    {
        var raw = session.FinalObservations.LastOrDefault() ?? session.PendingFinal ?? session.ConfirmedFinal;
        if (raw is null) return "Not observed";
        var evidence = raw.PlacementEvidence.Where(e => e.Source != ReadingSource.Manual).ToArray();
        if (evidence.Length == 0) return "Not observed";
        var places = evidence.Select(e => e.PossiblePlaces.AsEnumerable()).Aggregate((a, b) => a.Intersect(b)).Order().ToArray();
        if (places.Length == 0) return "Conflicting observations";
        return places.SequenceEqual(new[] { 2, 3 }) ? "2nd or 3rd place"
            : places.Length == 1 ? $"{Ordinal(places[0])} place" : "Placement not established";
    }

    private void OpenSavedResults_Click(object sender, RoutedEventArgs e)
    {
        MainTabs.SelectedItem = SavedResultsTab;
        RefreshSavedResults();
    }

    private async void ReviewSavedResult_Click(object sender, RoutedEventArgs e)
    {
        if (HasPendingSavedResultDeletionSave) return;
        if (busy || !trackerReady || isClosing || switchingSeason || ResultsOwner is not { } owner
            || sender is not FrameworkElement { Tag: Guid id }) return;
        var session = owner.Document.Sessions.FirstOrDefault(s => s.Id == id);
        if (session is null) return;
        if (session.PendingFinal is not null)
        {
            if (persistenceBlocked && !ReferenceEquals(owner, fakeAnalyticsTracker))
            { SavedResultsStatus.Text = "Resolve the save error before changing this result."; return; }
            await ReviewFinalAsync(owner, session);
        }
        else
        {
            SetBusy(true);
            try
            {
                if (session.ConfirmedFinal is not null) MatchSummaryDialog.Show(this, session);
                else ShowObservedBackup(session);
            }
            finally { FinishWork(); }
        }
    }

    private async Task ReviewFinalAsync(SessionTracker owner, TrackerSession session)
    {
        SetBusy(true);
        try
        {
            while (session.PendingFinal is { } pending)
            {
                if (pending.Tracks.Count == 0)
                {
                    var action = TrackerDialogs.Choose(this, "Final XP not captured",
                        "Only the end screen or placement is saved. No final XP total was captured. Read the match-end PROGRESSION tab. In the walkthrough, read mock 12 next. For an older match, enter a final total only if you recorded it at that match's end.",
                        "Close — capture final XP first", "Enter a recorded final total…", "View observed backup…");
                    if (action == 1) { if (await CorrectFinalAsync(owner, session)) continue; }
                    if (action == 2) { ShowObservedBackup(session); continue; }
                    return;
                }
                var choice = TrackerDialogs.Choose(this, "Record Final XP results?",
                    $"Match started {session.StartedAt.ToLocalTime():g}. Final cumulative totals: {FinalTotalsText(pending)}. These totals include any bonuses. Use the observed placement, or select a placement only if you know it. Observed values stay saved as a backup.",
                    $"Use observed: {ObservedPlacementText(session)}", "1st place", "2nd place", "3rd place",
                    "Correct a final XP total…", "View observed backup…");
                if (choice is null) return;
                if (choice == 4) { if (await CorrectFinalAsync(owner, session)) continue; return; }
                if (choice == 5) { ShowObservedBackup(session); continue; }
                var result = owner.ConfirmFinalForSession(session.Id, choice == 0 ? null : choice);
                SessionNotice.Text = result.Message;
                SavedResultsStatus.Text = result.Message;
                if (!result.Applied)
                {
                    TrackerDialogs.Choose(this, "Final XP was not recorded", result.Message, "Close");
                    return;
                }
                if (result.Changed && !await SaveResultsOwnerAsync(owner)) return;
                await WalkthroughFinalConfirmedAsync(owner, session);
                if (ReferenceEquals(owner, tracker) && tracker.ActiveSession?.Id == session.Id) resumePending = false;
                RefreshTracker();
                RefreshSavedResults();
                MatchSummaryDialog.Show(this, session);
                return;
            }
        }
        finally { FinishWork(); }
    }

    private async Task<bool> CorrectFinalAsync(SessionTracker owner, TrackerSession session)
    {
        var classes = Enum.GetValues<XpClass>();
        var selected = TrackerDialogs.Choose(this, "Correct final cumulative XP",
            "Choose the class to correct for this saved match. Enter cumulative final XP, including bonuses. This does not change the live tracker or the observed backup.",
            classes.Select(c => c.ToString()).ToArray());
        if (selected is null) return false;
        var trackClass = classes[selected.Value];
        var value = TrackerDialogs.Xp(this, $"{trackClass} · saved final XP", "Enter the recorded cumulative final XP for this match. Do not use a current total from a later match.");
        if (value is null) return false;
        var result = owner.ManualFinalForSession(session.Id, trackClass, value.Value);
        SavedResultsStatus.Text = result.Message;
        if (!result.Applied)
        { TrackerDialogs.Choose(this, "Final XP was not changed", result.Message, "Close"); return false; }
        if (result.Changed && !await SaveResultsOwnerAsync(owner)) return false;
        RefreshTracker();
        RefreshSavedResults();
        return true;
    }

    private async Task<bool> SaveResultsOwnerAsync(SessionTracker owner, string action = "Final XP")
    {
        if (HasPendingSavedResultDeletionSave) return false;
        if (!ReferenceEquals(owner, fakeAnalyticsTracker))
        {
            await PersistTrackerAsync(owner);
            if (!persistenceBlocked) return true;
            SavedResultsStatus.Text = "Confirmation remains in memory. The save failed. Keep the app open and use Retry save.";
            TrackerDialogs.Choose(this, $"{action} save failed", SaveStatus.Text, "Close");
            return false;
        }
        try
        {
            await MockStore.SaveAsync(fakeResultsFilename, owner.Document);
            SavedResultsStatus.Text = "Fake results saved in the separate mock folder.";
            return true;
        }
        catch (Exception exception)
        {
            SavedResultsStatus.Text = $"Fake result save failed: {exception.Message}. Real data is unchanged.";
            TrackerDialogs.Choose(this, "Fake result save failed", SavedResultsStatus.Text, "Close");
            return false;
        }
    }

    private void ObservedBackup_Click(object sender, RoutedEventArgs e)
    {
        if (busy || ResultsOwner is not { } owner || sender is not FrameworkElement { Tag: Guid id }) return;
        var session = owner.Document.Sessions.FirstOrDefault(s => s.Id == id);
        if (session is null) return;
        SetBusy(true);
        try { ShowObservedBackup(session); }
        finally { FinishWork(); }
    }

    private void ShowObservedBackup(TrackerSession session)
    {
        var panel = new StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(new TextBlock { Text = $"Match {session.StartedAt.ToLocalTime():g}", FontSize = 22, FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = "Observed backup — read only. Captured revisions remain separate from confirmed totals and manual changes.", Margin = new Thickness(0, 8, 0, 12), Foreground = Brushes.LightSteelBlue });
        panel.Children.Add(new TextBlock { Text = "Starting totals: " + string.Join(" · ", session.Tracks.Where(p => p.Value.OriginalBaseline is not null).Select(p => $"{p.Key} {p.Value.OriginalBaseline!.Xp:N0}")), Margin = new Thickness(0, 0, 0, 10) });
        panel.Children.Add(new TextBlock { Text = "Pending totals: " + FinalTotalsText(session.PendingFinal) + "\nConfirmed totals: " + FinalTotalsText(session.ConfirmedFinal), Margin = new Thickness(0, 0, 0, 10) });
        if (session.FinalObservations.Count == 0)
            panel.Children.Add(new TextBlock { Text = "No separate observed revision was stored in this older record. The pending/confirmed totals and saved observation history remain available." });
        foreach (var revision in session.FinalObservations.AsEnumerable().Reverse())
        {
            panel.Children.Add(new TextBlock { Text = $"Captured {revision.LastObservedAt.ToLocalTime():g} · " + FinalTotalsText(revision), Margin = new Thickness(0, 9, 0, 3), FontWeight = FontWeights.SemiBold });
            panel.Children.Add(new TextBlock { Text = "Placement text: " + (revision.PlacementEvidence.Count == 0 ? "Not observed" : string.Join(" · ", revision.PlacementEvidence.Select(e => e.RawText).Distinct())), Foreground = Brushes.LightSteelBlue });
        }
        var area = SystemParameters.WorkArea;
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.Children.Add(new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Visible, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        var close = new Button { Content = "Close", IsCancel = true, IsDefault = true, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(20) };
        Grid.SetRow(close, 1); grid.Children.Add(close);
        var dialog = new Window { Owner = this, Title = "Saved observed results", Width = Math.Min(760, area.Width - 24), Height = Math.Min(620, area.Height - 24), Content = grid,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Style = (Style)FindResource(typeof(Window)) };
        close.Click += (_, _) => dialog.Close();
        dialog.ShowDialog();
    }
}
