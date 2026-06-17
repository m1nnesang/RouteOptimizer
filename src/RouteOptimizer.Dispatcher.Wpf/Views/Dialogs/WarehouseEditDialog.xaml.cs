using System.Windows;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf.Views.Dialogs;

public partial class WarehouseEditDialog : Window
{
    public WarehouseEditDialog(WarehouseEditDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
