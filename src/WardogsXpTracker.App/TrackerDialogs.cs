using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace WardogsXpTracker.App;

internal static class TrackerDialogs
{
    private static Window Window(Window owner, string title, StackPanel panel) => new()
    {
        Owner = owner, Title = title, Width = 470, SizeToContent = SizeToContent.Height,
        ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterOwner,
        Content = panel, Style = (Style)owner.FindResource(typeof(Window))
    };

    public static int? Choose(Window owner, string title, string message, params string[] choices)
    {
        var panel = new StackPanel { Margin = new Thickness(22) };
        panel.Children.Add(new TextBlock { Text = message, Margin = new Thickness(0, 0, 0, 16) });
        var dialog = Window(owner, title, panel);
        int? answer = null;
        for (var i = 0; i < choices.Length; i++)
        {
            var index = i;
            var button = new Button { Content = choices[i], Margin = new Thickness(0, 0, 0, 8) };
            button.Click += (_, _) => { answer = index; dialog.DialogResult = true; };
            panel.Children.Add(button);
        }
        dialog.ShowDialog();
        return answer;
    }

    public static long? Xp(Window owner, string title, string message)
    {
        var panel = new StackPanel { Margin = new Thickness(22) };
        panel.Children.Add(new TextBlock { Text = message, Margin = new Thickness(0, 0, 0, 14) });
        var input = new TextBox { FontSize = 23, Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 10) };
        var error = new TextBlock { Foreground = System.Windows.Media.Brushes.Salmon, Margin = new Thickness(0, 0, 0, 10) };
        panel.Children.Add(input); panel.Children.Add(error);
        var dialog = Window(owner, title, panel);
        long? result = null;
        var okay = new Button { Content = "Record XP", IsDefault = true, Margin = new Thickness(0, 0, 0, 8) };
        okay.Click += (_, _) =>
        {
            var text = input.Text.Trim().Replace(",", "", StringComparison.Ordinal);
            if (text.Length == 0 || !text.All(char.IsAsciiDigit) || !long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var xp))
            { error.Text = "Enter a nonnegative whole XP number."; return; }
            result = xp; dialog.DialogResult = true;
        };
        var cancel = new Button { Content = "Cancel", IsCancel = true };
        panel.Children.Add(okay); panel.Children.Add(cancel);
        dialog.Loaded += (_, _) => input.Focus();
        dialog.ShowDialog();
        return result;
    }
}
