using System.Windows;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf.Views.Dialogs;

public partial class CreateShiftDialog : Window
{
    public CreateShiftDialog(CreateShiftDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
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
