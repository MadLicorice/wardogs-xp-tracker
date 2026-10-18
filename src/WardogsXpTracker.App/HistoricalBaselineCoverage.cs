using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WardogsXpTracker.App.Testing;
using WardogsXpTracker.Core.Reading;
using WardogsXpTracker.Core.Tracking;

namespace WardogsXpTracker.App;

public partial class MainWindow
{
    private string fakeResultsFilename = "demo-analytics-season.md";

    private async void LoadBaselineCoverageDemo_Click(object sender, RoutedEventArgs e)
    {
        if (!TestToolsEnabled || !CanChangeTracker()) return;
        SetBusy(true);
        try
        {
            if (fakeAnalyticsSeason is not null)
                await MockStore.SaveAsync(fakeResultsFilename, fakeAnalyticsSeason);
            var document = HistoricalCoverageDemoFactory.CreateSeason();
            var replacement = new SessionTracker(document);
            const string filename = "history-baseline-workflow.md";
            await MockStore.SaveAsync(filename, document);
            fakeAnalyticsSeason = document;
            fakeAnalyticsTracker = replacement;
            fakeResultsFilename = filename;
            ClearImportedAnalytics();
            RefreshSavedResults();
            MainTabs.SelectedItem = SavedResultsTab;
            SavedResultsStatus.Text = "MOCK: completed match with unconfirmed starting baselines. Open Starting baselines… "
                + "Confirm Assault, then all remaining eligible baselines. Reopen Summary to see the updated source split. Real data is unchanged.";
            TestStatus.Text = SavedResultsStatus.Text;
        }
        catch (Exception exception) { TestStatus.Text = $"Starting-baseline mock could not load: {exception.Message}"; }
        finally { FinishWork(); }
    }

    private void ReviewSavedBaselines_Click(object sender, RoutedEventArgs e)
    {
        if (HasPendingSavedResultDeletionSave) return;
        if (busy || !trackerReady || isClosing || switchingSeason || ResultsOwner is not { } owner
            || sender is not FrameworkElement { Tag: Guid id }) return;
        if (persistenceBlocked && !ReferenceEquals(owner, fakeAnalyticsTracker))
        { SavedResultsStatus.Text = "Resolve the save error before confirming starting baselines."; return; }
        var session = owner.Document.Sessions.FirstOrDefault(item => item.Id == id);
        if (session is null) return;
        SetBusy(true);
        try { ShowSavedBaselineCoverage(owner, session); }
        finally { FinishWork(); }
    }

    private void ShowSavedBaselineCoverage(SessionTracker owner, TrackerSession session)
    {
        var area = SystemParameters.WorkArea;
        var root = new Grid { Margin = new Thickness(18) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var heading = new StackPanel();
        heading.Children.Add(new TextBlock { Text = "Confirm saved starting baselines", FontSize = 22, FontWeight = FontWeights.SemiBold });
        heading.Children.Add(new TextBlock
        {
            Text = $"Match started {session.StartedAt.ToLocalTime():g}. Confirm only an original baseline that preceded all earned XP in its class. "
                + "This updates analytics coverage. It does not change XP totals, final results, or the active match.",
            Margin = new Thickness(0, 8, 0, 12), TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightSteelBlue
        });
        root.Children.Add(heading);
        var rows = new StackPanel { Name = "SavedCoverageRows" };
        var scroll = new ScrollViewer { Content = rows, VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        Grid.SetRow(scroll, 1); root.Children.Add(scroll);
        var footer = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
        var status = new TextBlock { Name = "SavedCoverageStatus", TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightSteelBlue,
            Margin = new Thickness(0, 0, 0, 9) };
        footer.Children.Add(status);
        var actions = new WrapPanel();
        var all = new Button { Name = "ConfirmAllSavedBaselines", Content = "Confirm all eligible baselines…", Margin = new Thickness(0, 0, 8, 7) };
        var summary = new Button { Name = "ViewSavedCoverageSummary", Content = "View XP summary", IsEnabled = session.ConfirmedFinal is not null,
            Margin = new Thickness(0, 0, 8, 7) };
        var retry = new Button { Name = "RetrySavedCoverageSave", Content = "Retry save", Visibility = Visibility.Collapsed,
            Margin = new Thickness(0, 0, 8, 7) };
        var close = new Button { Name = "CloseSavedCoverage", Content = "Close", IsCancel = true, Margin = new Thickness(0, 0, 0, 7) };
        actions.Children.Add(all); actions.Children.Add(summary); actions.Children.Add(retry); actions.Children.Add(close); footer.Children.Add(actions);
        Grid.SetRow(footer, 2); root.Children.Add(footer);
        var dialog = new Window
        {
            Owner = this, Title = "Saved starting baselines", Width = Math.Min(720, Math.Max(320, area.Width - 24)),
            Height = Math.Min(660, Math.Max(300, area.Height - 24)), MinWidth = Math.Min(540, Math.Max(320, area.Width - 24)),
            Content = root, WindowStartupLocation = WindowStartupLocation.CenterOwner, Style = (Style)FindResource(typeof(Window))
        };
        var saving = false;
        var saveFailed = false;
        void Refresh()
        {
            rows.Children.Clear();
            var eligible = owner.GetConfirmableCoverageClasses(session.Id).ToHashSet();
            foreach (var trackClass in Enum.GetValues<XpClass>())
            {
                var track = session.Tracks[trackClass];
                var original = track.OriginalBaseline;
                var confirmed = original is { Validity: BaselineValidity.Valid, Coverage: BaselineCoverage.BeforeEarnedXp };
                var text = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
                text.Children.Add(new TextBlock { Text = trackClass.ToString(), FontSize = 16, FontWeight = FontWeights.SemiBold });
                text.Children.Add(new TextBlock { Text = original is null ? "Original starting XP not recorded" : $"Original starting XP: {original.Xp:N0}",
                    FontSize = 12, Margin = new Thickness(0, 3, 0, 2) });
                text.Children.Add(new TextBlock
                {
                    Text = SavedBaselineCoverageStatus(track, eligible.Contains(trackClass)), FontSize = 12,
                    TextWrapping = TextWrapping.Wrap, Foreground = confirmed ? Brushes.LightGreen : Brushes.LightSteelBlue
                });
                var button = new Button { Name = "ConfirmSavedBaseline_" + trackClass, Content = confirmed ? "Confirmed" : "Confirm…",
                    Tag = trackClass, IsEnabled = eligible.Contains(trackClass) && !saveFailed, VerticalAlignment = VerticalAlignment.Center };
                button.Click += async (_, _) => await ConfirmAsync(trackClass);
                var row = new DockPanel(); DockPanel.SetDock(button, Dock.Right); row.Children.Add(button); row.Children.Add(text);
                rows.Children.Add(new Border { Child = row, BorderBrush = new SolidColorBrush(Color.FromRgb(55, 64, 76)),
                    BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(0, 9, 8, 9) });
            }
            all.IsEnabled = eligible.Count > 0 && !saveFailed;
            summary.IsEnabled = session.ConfirmedFinal is not null && !saveFailed;
            retry.Visibility = saveFailed ? Visibility.Visible : Visibility.Collapsed;
            status.Text = eligible.Count == 0 ? "No eligible unconfirmed starting baselines remain. Existing XP totals and observations are unchanged."
                : $"{eligible.Count} original starting baseline(s) can be confirmed. Each action asks for confirmation.";
        }
        async Task ConfirmAsync(XpClass? trackClass)
        {
            if (saving) return;
            var eligible = owner.GetConfirmableCoverageClasses(session.Id)
                .Where(value => trackClass is null || value == trackClass).ToArray();
            if (eligible.Length == 0) { Refresh(); return; }
            var baselineVersions = eligible.ToDictionary(value => value,
                value => (session.Tracks[value].OriginalBaseline!.Id, session.Tracks[value].OriginalBaseline!.Version));
            var list = string.Join(Environment.NewLine, eligible.Select(value => $"{value}: {session.Tracks[value].OriginalBaseline!.Xp:N0} XP"));
            var message = $"Match started {session.StartedAt.ToLocalTime():g}.{Environment.NewLine}{list}{Environment.NewLine}{Environment.NewLine}"
                + "Each listed value was captured before I earned any XP in that class during this saved match. "
                + "This confirms starting coverage for analytics. It does not change totals or confirm bonus receipt.";
            if (TrackerDialogs.Choose(dialog, trackClass is { } named ? $"Confirm saved {named} starting XP" : "Confirm all saved starting baselines",
                message, trackClass.HasValue ? $"Confirm {trackClass.Value}" : "Confirm all listed baselines", "Cancel") != 0) return;
            if (!owner.Document.Sessions.Contains(session) || baselineVersions.Any(pair => session.Tracks[pair.Key].OriginalBaseline is not { } baseline
                || (baseline.Id, baseline.Version) != pair.Value))
            { Refresh(); status.Text = "A saved baseline changed. Review the values before confirming again."; return; }
            saving = true; root.IsEnabled = false;
            try
            {
                var result = trackClass is { } selected ? owner.ConfirmCoverageForSession(session.Id, selected)
                    : owner.ConfirmCoverageForSession(session.Id);
                SavedResultsStatus.Text = result.Message;
                if (!result.Applied) { status.Text = result.Message; return; }
                if (result.Changed && !await SaveResultsOwnerAsync(owner, "Starting-baseline confirmation"))
                {
                    saveFailed = true; Refresh();
                    status.Text = "Confirmation remains in memory. The save failed. Use Retry save in this view before continuing.";
                    return;
                }
                RefreshTracker(); RefreshSavedResults(); RefreshAnalyticsIfVisible(); Refresh();
                status.Text = "Starting-baseline confirmation saved. View XP summary or Analytics for the updated source split. Live tracking is unchanged.";
            }
            catch (Exception exception)
            { saveFailed = true; Refresh(); status.Text = $"Starting-baseline confirmation could not save: {exception.Message}"; }
            finally { saving = false; root.IsEnabled = true; }
        }
        all.Click += async (_, _) => await ConfirmAsync(null);
        retry.Click += async (_, _) =>
        {
            if (saving) return;
            saving = true; root.IsEnabled = false;
            try
            {
                if (!ReferenceEquals(owner, fakeAnalyticsTracker)) persistenceBlocked = false;
                saveFailed = !await SaveResultsOwnerAsync(owner, "Starting-baseline confirmation");
                RefreshTracker(); RefreshSavedResults(); RefreshAnalyticsIfVisible(); Refresh();
                status.Text = saveFailed ? "The save still failed. Confirmation remains in memory. Keep the app open and retry."
                    : "Starting-baseline confirmation saved. View XP summary or Analytics for the updated source split.";
            }
            catch (Exception exception) { saveFailed = true; Refresh(); status.Text = $"Save could not complete: {exception.Message}"; }
            finally { saving = false; root.IsEnabled = true; }
        };
        summary.Click += (_, _) => MatchSummaryDialog.Show(dialog, session);
        close.Click += (_, _) => dialog.Close();
        dialog.Closing += (_, args) => { if (saving) args.Cancel = true; };
        Refresh(); dialog.ShowDialog();
    }

    private static string SavedBaselineCoverageStatus(TrackState track, bool canConfirm)
    {
        if (!track.Enabled) return "This class was not tracked in this match.";
        if (track.OriginalBaseline is not { } original) return "No original baseline is available to confirm.";
        if (original is { Validity: BaselineValidity.Valid, Coverage: BaselineCoverage.BeforeEarnedXp }) return "Starting baseline confirmed for analytics.";
        if (canConfirm) return "Starting baseline timing remains unconfirmed.";
        if (track.WasRebased) return "A rebase prevents confirmation of this saved starting baseline.";
        if (original.Validity == BaselineValidity.Invalid) return "The original baseline is marked invalid.";
        if (original.Coverage == BaselineCoverage.Partial) return "The saved baseline covers only part of this match.";
        return "This baseline is not eligible for starting-coverage confirmation.";
    }
}
