using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace WardogsXpTracker.App;

/// <summary>Lets read-only tables release selection and share wheel input with their page.</summary>
public static class TableInteraction
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(TableInteraction), new PropertyMetadata(false, EnabledChanged));

    private static readonly DependencyProperty RegistrationProperty = DependencyProperty.RegisterAttached(
        "Registration", typeof(Registration), typeof(TableInteraction));

    public static bool GetEnabled(DependencyObject element) => (bool)element.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject element, bool value) => element.SetValue(EnabledProperty, value);

    private static void EnabledChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not DataGrid grid) return;
        if ((bool)e.NewValue)
        {
            if (grid.GetValue(RegistrationProperty) is Registration) return;
            var registration = new Registration(grid);
            grid.SetValue(RegistrationProperty, registration);
            registration.Enable();
        }
        else if (grid.GetValue(RegistrationProperty) is Registration registration)
        {
            registration.Disable();
            grid.ClearValue(RegistrationProperty);
        }
    }

    private sealed class Registration(DataGrid grid)
    {
        private Window? window;

        public void Enable()
        {
            grid.Loaded += Loaded;
            grid.Unloaded += Unloaded;
            grid.LostKeyboardFocus += LostKeyboardFocus;
            grid.PreviewMouseWheel += PreviewMouseWheel;
            if (grid.IsLoaded) AttachWindow();
        }

        public void Disable()
        {
            grid.Loaded -= Loaded;
            grid.Unloaded -= Unloaded;
            grid.LostKeyboardFocus -= LostKeyboardFocus;
            grid.PreviewMouseWheel -= PreviewMouseWheel;
            DetachWindow();
        }

        private void Loaded(object sender, RoutedEventArgs e) => AttachWindow();
        private void Unloaded(object sender, RoutedEventArgs e) => DetachWindow();

        private void AttachWindow()
        {
            var owner = Window.GetWindow(grid);
            if (ReferenceEquals(window, owner)) return;
            DetachWindow();
            window = owner;
            if (window is null) return;
            window.AddHandler(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(WindowPointerDown), true);
            window.Deactivated += WindowDeactivated;
        }

        private void DetachWindow()
        {
            if (window is null) return;
            window.RemoveHandler(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(WindowPointerDown));
            window.Deactivated -= WindowDeactivated;
            window = null;
        }

        private void WindowPointerDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsWithin(e.OriginalSource as DependencyObject, grid)) ClearSelection();
        }

        private void WindowDeactivated(object? sender, EventArgs e) => ClearSelection();

        private void LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (IsWithin(e.NewFocus as DependencyObject, grid)) return;
            _ = grid.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                if (GetEnabled(grid) && !grid.IsKeyboardFocusWithin) ClearSelection();
            }));
        }

        private void ClearSelection()
        {
            grid.UnselectAll();
            grid.UnselectAllCells();
            grid.CurrentCell = new DataGridCellInfo();
        }

        private void PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled || e.Delta == 0) return;
            var inner = grid.Template?.FindName("DG_ScrollViewer", grid) as ScrollViewer ?? FindScrollViewer(grid);
            if (TryScroll(inner, e.Delta)) { e.Handled = true; return; }
            for (var parent = Parent(grid); parent is not null; parent = Parent(parent))
            {
                if (parent is ScrollViewer viewer && TryScroll(viewer, e.Delta))
                { e.Handled = true; return; }
            }
        }
    }

    private static bool TryScroll(ScrollViewer? viewer, int delta)
    {
        if (viewer is null || viewer.VerticalScrollBarVisibility == ScrollBarVisibility.Disabled) return false;
        var canMove = delta < 0 ? viewer.VerticalOffset < viewer.ScrollableHeight - .001 : viewer.VerticalOffset > .001;
        if (!canMove) return false;
        var lines = SystemParameters.WheelScrollLines;
        if (lines == 0) return false;
        var notches = Math.Max(1, (int)Math.Ceiling(Math.Abs((double)delta) / 120));
        if (lines < 0)
        {
            for (var i = 0; i < notches; i++)
                if (delta < 0) viewer.PageDown(); else viewer.PageUp();
        }
        else
        {
            for (var i = 0; i < lines * notches; i++)
                if (delta < 0) viewer.LineDown(); else viewer.LineUp();
        }
        return true;
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is ScrollViewer viewer) return viewer;
            if (FindScrollViewer(child) is { } nested) return nested;
        }
        return null;
    }

    private static bool IsWithin(DependencyObject? element, DependencyObject ancestor)
    {
        for (var current = element; current is not null; current = Parent(current))
            if (ReferenceEquals(current, ancestor)) return true;
        return false;
    }

    private static DependencyObject? Parent(DependencyObject element)
    {
        if (element is Visual or Visual3D)
            return VisualTreeHelper.GetParent(element) ?? LogicalTreeHelper.GetParent(element);
        if (element is ContentElement content)
            return ContentOperations.GetParent(content) ?? (content as FrameworkContentElement)?.Parent;
        return LogicalTreeHelper.GetParent(element);
    }
}
