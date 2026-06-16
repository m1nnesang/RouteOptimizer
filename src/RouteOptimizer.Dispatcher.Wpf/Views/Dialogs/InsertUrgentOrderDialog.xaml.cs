using System.Windows;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf.Views.Dialogs;

public partial class InsertUrgentOrderDialog : Window
{
    public InsertUrgentOrderDialog(InsertUrgentOrderDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is InsertUrgentOrderDialogViewModel vm)
            vm.LoadOrdersCommand.Execute(null);
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
