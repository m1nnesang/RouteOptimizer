using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RouteOptimizer.Dispatcher.Wpf.Views;

public partial class RoutesView : UserControl
{
    public RoutesView()
    {
        InitializeComponent();
    }

    private void StopsGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var cell = FindAncestor<DataGridCell>(e.OriginalSource as DependencyObject);
        if (cell is null)
            return;

        var row = FindAncestor<DataGridRow>(cell);
        if (row is null || !row.IsSelected)
            return;

        ((DataGrid)sender).SelectedItem = null;
        e.Handled = true;
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
                return match;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
