using System.Windows;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf.Views.Dialogs;

public partial class CreateRouteDialog : Window
{
    public CreateRouteDialog(CreateRouteDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CreateRouteDialogViewModel vm)
            vm.LoadOrdersCommand.Execute(null);
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        // Commit any in-progress DatePicker text edit to the view model before reading it.
        (sender as System.Windows.Controls.Button)?.Focus();
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
