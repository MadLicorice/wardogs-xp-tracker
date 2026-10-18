using System.Windows;
using System.Windows.Controls;

namespace WardogsXpTracker.App;

public partial class MainWindow
{
    private int layoutProfile = -1;

    private void AppRoot_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyLayoutProfile(e.NewSize.Width);
    }

    private void ApplyLayoutProfile(double width)
    {
        if (TrackerLayoutGrid is null || width <= 0 || !double.IsFinite(width)) return;
        var next = width < 1100 ? 0 : width < 1600 ? 1 : 2;
        if (layoutProfile == next) return;
        layoutProfile = next;
        TrackerLayoutGrid.ColumnDefinitions[1].Width = new(next == 0 ? 8 : 12);
        TrackerLayoutGrid.ColumnDefinitions[2].Width = new(next == 0 ? 260 : next == 1 ? 300 : 340);
    }

    private void UpdatePageScrollMode()
    {
        if (MainScrollViewer is null || SessionTrackerTab is null) return;
        MainScrollViewer.ScrollToTop();
        MainScrollViewer.VerticalScrollBarVisibility = SessionTrackerTab.IsSelected
            ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Visible;
    }
}
