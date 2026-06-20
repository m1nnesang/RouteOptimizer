using System.Windows;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.ViewModels;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;
using RouteOptimizer.Dispatcher.Wpf.Views.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf.Services;

public class WpfDialogService : IDialogService
{
    public void ShowDeliveryHistoryDialog(OrderDeliveryHistoryViewModel viewModel)
    {
        var dialog = new DeliveryHistoryDialog(viewModel) { Owner = Application.Current.MainWindow };
        dialog.ShowDialog();
    }

    public bool? ShowCreateOrderDialog(CreateOrderDialogViewModel viewModel)
    {
        var dialog = new CreateOrderDialog(viewModel) { Owner = Application.Current.MainWindow };
        return dialog.ShowDialog();
    }

    public bool? ShowCreateRouteDialog(CreateRouteDialogViewModel viewModel)
    {
        var dialog = new CreateRouteDialog(viewModel) { Owner = Application.Current.MainWindow };
        return dialog.ShowDialog();
    }

    public bool? ShowAssignShiftDialog(AssignShiftDialogViewModel viewModel)
    {
        var dialog = new AssignShiftDialog(viewModel) { Owner = Application.Current.MainWindow };
        return dialog.ShowDialog();
    }

    public bool? ShowInsertUrgentOrderDialog(InsertUrgentOrderDialogViewModel viewModel)
    {
        var dialog = new InsertUrgentOrderDialog(viewModel) { Owner = Application.Current.MainWindow };
        return dialog.ShowDialog();
    }

    public bool? ShowHandoverDialog(HandoverDialogViewModel viewModel)
    {
        var dialog = new HandoverDialog(viewModel) { Owner = Application.Current.MainWindow };
        return dialog.ShowDialog();
    }

    public bool? ShowVehicleEditDialog(VehicleEditDialogViewModel viewModel)
    {
        var dialog = new VehicleEditDialog(viewModel) { Owner = Application.Current.MainWindow };
        return dialog.ShowDialog();
    }

    public bool? ShowWarehouseEditDialog(WarehouseEditDialogViewModel viewModel)
    {
        var dialog = new WarehouseEditDialog(viewModel) { Owner = Application.Current.MainWindow };
        return dialog.ShowDialog();
    }

    public bool ShowConfirm(string message, string title) =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
}
