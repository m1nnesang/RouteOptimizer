using RouteOptimizer.Dispatcher.Wpf.ViewModels;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

public interface IDialogService
{
    void ShowDeliveryHistoryDialog(OrderDeliveryHistoryViewModel viewModel);
    bool? ShowCreateOrderDialog(CreateOrderDialogViewModel viewModel);
    bool? ShowCreateRouteDialog(CreateRouteDialogViewModel viewModel);
    bool? ShowAssignShiftDialog(AssignShiftDialogViewModel viewModel);
    bool? ShowInsertUrgentOrderDialog(InsertUrgentOrderDialogViewModel viewModel);
    bool? ShowHandoverDialog(HandoverDialogViewModel viewModel);
    bool? ShowVehicleEditDialog(VehicleEditDialogViewModel viewModel);
    bool? ShowWarehouseEditDialog(WarehouseEditDialogViewModel viewModel);
    bool ShowConfirm(string message, string title);
}
