using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WardogsXpTracker.App;

public sealed record OnboardingContext(string SeasonName, string SourceLabel, string SessionHint,
    string SnapshotShortcut, string CheckShortcut, long TargetXp, bool SoundEnabled);

/// <summary>Explains the first match without recording XP or changing a session.</summary>
public static class OnboardingDialog
{
    public static bool? Show(Window owner, OnboardingContext context, Func<bool, Task> saveProgress)
    {
        var area = SystemParameters.WorkArea;
        var dialog = new Window
        {
            Name = "OnboardingWindow", Owner = owner, Title = "Welcome to WARDOGS XP Tracker",
            Width = Math.Min(680, Math.Max(280, area.Width - 24)),
            Height = Math.Min(560, Math.Max(240, area.Height - 24)),
            ResizeMode = ResizeMode.NoResize, ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Style = (Style)owner.FindResource(typeof(Window))
        };
        var layout = new Grid { Margin = new Thickness(24) };
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition());
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        header.Children.Add(Text("MadLicorice's WARDOGS XP Tracker", 14, "#F1CF69", FontWeights.SemiBold));
        var progress = Text("", 13, "#A8B0BC"); progress.Name = "OnboardingProgressText";
        progress.Margin = new Thickness(0, 6, 0, 0); header.Children.Add(progress);
        layout.Children.Add(header);
        var content = new StackPanel { Name = "OnboardingPageContent", Margin = new Thickness(0, 0, 12, 0) };
        var scroll = new ScrollViewer { Name = "OnboardingBodyScroll", Content = content,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        Grid.SetRow(scroll, 1); layout.Children.Add(scroll);
        var footer = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
        var note = Text("Reopen this guide in Options → Other Options. Your saved XP stays unchanged.", 12, "#A8B0BC");
        note.Margin = new Thickness(0, 0, 0, 10); footer.Children.Add(note);
        var error = Text("", 13, "#F0AF92"); error.Name = "OnboardingSaveError"; error.Visibility = Visibility.Collapsed;
        error.Margin = new Thickness(0, 0, 0, 10); footer.Children.Add(error);
        var recovery = new WrapPanel { Visibility = Visibility.Collapsed, Margin = new Thickness(0, 0, 0, 8) };
        var retry = Button("OnboardingRetryButton", "Retry save", 100);
        var unsaved = Button("OnboardingCloseWithoutSavingButton", "Close without saving", 170);
        recovery.Children.Add(retry); recovery.Children.Add(unsaved); footer.Children.Add(recovery);
        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        var back = Button("OnboardingBackButton", "Back", 84);
        var skip = Button("OnboardingSkipButton", "Skip for now", 116);
        var next = Button("OnboardingNextButton", "Next", 100);
        var finish = Button("OnboardingFinishButton", "Finish", 100);
        finish.Background = Brush("#344638"); finish.BorderBrush = Brush("#76D8AE");
        actions.Children.Add(back); actions.Children.Add(skip); actions.Children.Add(next); actions.Children.Add(finish);
        footer.Children.Add(actions); Grid.SetRow(footer, 2); layout.Children.Add(footer);
        dialog.Content = layout;
        var page = 0;
        var saving = false;
        var allowClose = false;
        var lastCompletion = false;
        bool? completedResult = null;

        void ShowPage()
        {
            content.Children.Clear(); scroll.ScrollToTop();
            progress.Text = $"Step {page + 1} of 4";
            back.IsEnabled = page > 0;
            next.Visibility = page < 3 ? Visibility.Visible : Visibility.Collapsed;
            finish.Visibility = page == 3 ? Visibility.Visible : Visibility.Collapsed;
            next.IsDefault = page < 3; finish.IsDefault = page == 3;
            var title = page switch { 0 => "Prepare the tracker", 1 => "Read your class XP",
                2 => "Record the match result", _ => "Analytics is optional" };
            var heading = Text(title, 26, "#F1F5F8", FontWeights.Bold);
            heading.Name = "OnboardingPageHeading"; heading.Margin = new Thickness(0, 0, 0, 12); content.Children.Add(heading);
            switch (page)
            {
                case 0:
                    var source = Text($"Season: {context.SeasonName} · {context.SourceLabel}", 13, "#9FD6E8");
                    source.Name = "OnboardingSourceText"; source.Margin = new Thickness(0, 0, 0, 12); content.Children.Add(source);
                    Callout(content, context.SessionHint, "#F1CF69");
                    Instruction(content, "1", "Select WARDOGS", "Choose the game under GAME WINDOW. Select Refresh if it is missing. You can finish this guide without the game open.");
                    Instruction(content, "2", "Start the right match", "Select New Session for each new match. The capture keys need a session before they can record XP.");
                    Instruction(content, "3", "Choose your classes", "Keep Track checked for the classes you want to measure. Each class can start as soon as its baseline exists.");
                    break;
                case 1:
                    Paragraph(content, "In WARDOGS, open TAB → UNLOCK → a class. Show its current XP column before each read.");
                    Instruction(content, context.SnapshotShortcut, "Snapshot the baseline", "Record starting XP before you earn XP in that class. Snapshot fills missing baselines and preserves existing ones.");
                    Instruction(content, context.CheckShortcut, "Check current XP", "Read the same class again after earning XP. Repeat for any other tracked class.");
                    Callout(content, $"A green check means {context.TargetXp:N0} XP earned since the baseline. Match-end bonuses do not advance that target. "
                        + (context.SoundEnabled ? "The target sound is enabled." : "The target sound is off. Change it in Options."), "#76D8AE");
                    Paragraph(content, "Completed reward pages do not show current XP. Change the UNLOCK column if the read is rejected. Manual XP is also available.", 13);
                    break;
                case 2:
                    Instruction(content, "1", "Read final XP", $"At match end, open PROGRESSION and use {context.CheckShortcut} to read final class totals. Those totals can include bonuses.");
                    Instruction(content, "2", "Record the result", "Select Record Final XP, then confirm only the placement you know. The summary shows the saved total and source breakdown.");
                    Instruction(content, "3", "Review later if needed", "Decide Later keeps the observations in Saved Results. You can review them after another session or a restart.");
                    Callout(content, "If you leave or disconnect, use End Session to record that outcome. Start New Session for the next match.", "#F1CF69");
                    break;
                case 3:
                    Callout(content, "The live XP target works without analytics confirmation.", "#76D8AE");
                    Instruction(content, "1", "Confirm starting coverage when you know it", "Use For Analytics ONLY on Session Tracker. Confirm only original baselines captured before any earned XP in their class.");
                    Instruction(content, "2", "Confirm an earlier match", "Open Saved Results → Starting baselines… to confirm eligible starts later. Confirm all has its own confirmation.");
                    Instruction(content, "3", "Read your statistics", "The separate Analytics tab shows Actions XP, Bonus XP, Indeterminate XP, and missed-bonus estimates. Final results are still required.");
                    break;
            }
        }

        async Task DismissAsync(bool completed)
        {
            if (saving || allowClose) return;
            saving = true; lastCompletion = completed;
            actions.IsEnabled = false; recovery.IsEnabled = false;
            error.Visibility = Visibility.Collapsed;
            try
            {
                await saveProgress(completed);
                completedResult = completed;
                allowClose = true;
                dialog.Close();
            }
            catch (Exception exception)
            {
                error.Text = $"Setup progress could not save: {exception.Message}\nRetry, or close without saving. This guide can appear again next launch.";
                error.Visibility = Visibility.Visible; recovery.Visibility = Visibility.Visible;
            }
            finally { saving = false; actions.IsEnabled = true; recovery.IsEnabled = true; }
        }

        back.Click += (_, _) => { if (page > 0) { page--; ShowPage(); } };
        next.Click += (_, _) => { if (page < 3) { page++; ShowPage(); } };
        skip.Click += async (_, _) => await DismissAsync(false);
        finish.Click += async (_, _) => await DismissAsync(true);
        retry.Click += async (_, _) => await DismissAsync(lastCompletion);
        unsaved.Click += (_, _) => { if (!saving) { allowClose = true; dialog.Close(); } };
        dialog.PreviewKeyDown += async (_, args) =>
        { if (args.Key == Key.Escape) { args.Handled = true; await DismissAsync(false); } };
        dialog.Closing += (_, args) =>
        {
            if (allowClose) return;
            args.Cancel = true;
            if (!saving) dialog.Dispatcher.BeginInvoke(new Action(async () => await DismissAsync(false)));
        };
        dialog.Loaded += (_, _) => next.Focus();
        ShowPage(); dialog.ShowDialog();
        return completedResult;
    }

    private static Button Button(string name, string caption, double width) => new()
    { Name = name, Content = caption, MinWidth = width, Margin = new Thickness(8, 4, 0, 0), Padding = new Thickness(12, 8, 12, 8) };
    private static TextBlock Text(string value, double size, string color, FontWeight? weight = null) => new()
    { Text = value, FontSize = size, Foreground = Brush(color), FontWeight = weight ?? FontWeights.Normal, TextWrapping = TextWrapping.Wrap };
    private static SolidColorBrush Brush(string color) => new((Color)ColorConverter.ConvertFromString(color));
    private static void Paragraph(Panel panel, string text, double size = 15)
    {
        var block = Text(text, size, "#BCC8D7"); block.Margin = new Thickness(0, 0, 0, 10); panel.Children.Add(block);
    }
    private static void Callout(Panel panel, string text, string color)
    {
        panel.Children.Add(new Border { Background = Brush("#1D2630"), CornerRadius = new CornerRadius(5),
            Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(0, 0, 0, 10),
            Child = Text(text, 14, color, FontWeights.SemiBold) });
    }
    private static void Instruction(Panel panel, string label, string title, string body)
    {
        var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition());
        var badge = Text(label, 14, "#F1CF69", FontWeights.Bold);
        badge.MinWidth = 24; badge.Margin = new Thickness(0, 0, 12, 0); row.Children.Add(badge);
        var text = new StackPanel(); text.Children.Add(Text(title, 16, "#EBEDF1", FontWeights.SemiBold));
        var detail = Text(body, 14, "#BCC8D7"); detail.Margin = new Thickness(0, 4, 0, 0); text.Children.Add(detail);
        Grid.SetColumn(text, 1); row.Children.Add(text); panel.Children.Add(row);
    }
}
