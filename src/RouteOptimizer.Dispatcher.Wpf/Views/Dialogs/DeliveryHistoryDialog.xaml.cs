using System.Windows;
using RouteOptimizer.Dispatcher.Wpf.ViewModels;

namespace RouteOptimizer.Dispatcher.Wpf.Views.Dialogs;

public partial class DeliveryHistoryDialog : Window
{
    public DeliveryHistoryDialog(OrderDeliveryHistoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
