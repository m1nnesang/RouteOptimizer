using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf.ViewModels;

public partial class OrdersViewModel : ObservableObject
{
    private readonly IApiHttpClient _apiHttpClient;
    private readonly IDialogService _dialogService;

    public OrdersViewModel(IApiHttpClient apiHttpClient, IDialogService dialogService)
    {
        _apiHttpClient = apiHttpClient;
        _dialogService = dialogService;
        DeliveryHistory = new OrderDeliveryHistoryViewModel(apiHttpClient);
        _ = LoadOrdersAsync();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial ObservableCollection<OrderListItem> Orders { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedOrder))]
    [NotifyPropertyChangedFor(nameof(NoOrderSelected))]
    public partial OrderListItem? SelectedOrder { get; set; }

    public OrderDeliveryHistoryViewModel DeliveryHistory { get; }

    public bool HasSelectedOrder => SelectedOrder is not null;

    public bool NoOrderSelected => SelectedOrder is null;

    partial void OnSelectedOrderChanged(OrderListItem? value)
    {
        if (value is not null)
            _ = DeliveryHistory.LoadAsync(value.Id);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDateFilter))]
    public partial DateTime? SelectedDate { get; set; }

    public bool HasDateFilter => SelectedDate is not null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool IsEmpty => !IsLoading && !HasError && Orders.Count == 0;

    partial void OnSelectedDateChanged(DateTime? value) => _ = LoadOrdersAsync();

    [RelayCommand]
    private void ClearDateFilter() => SelectedDate = null;

    [RelayCommand]
    private async Task LoadOrdersAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var url = "api/orders?pageSize=100";
            if (SelectedDate is not null)
                url += $"&date={SelectedDate.Value:yyyy-MM-dd}";

            var result = await _apiHttpClient.GetAsync<PagedResult<OrderListItem>>(url);
            Orders = new ObservableCollection<OrderListItem>(result?.Items ?? []);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to load orders. Please check your connection.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreateOrderAsync()
    {
        var warehouses = await _apiHttpClient.GetAsync<List<WarehouseListItem>>("api/warehouses") ?? [];
        if (warehouses.Count == 0)
        {
            ErrorMessage = "Create a warehouse before adding orders.";
            return;
        }

        var dialogViewModel = new CreateOrderDialogViewModel(warehouses, _apiHttpClient);
        if (_dialogService.ShowCreateOrderDialog(dialogViewModel) != true || dialogViewModel.OrderType is null)
            return;

        try
        {
            if (dialogViewModel.OrderType == OrderKind.Business)
                await _apiHttpClient.PostAsync<CreateBusinessOrderRequest, Guid>(
                    "api/orders/business", dialogViewModel.BuildBusinessRequest());
            else
                await _apiHttpClient.PostAsync<CreateIndividualOrderRequest, Guid>(
                    "api/orders/individual", dialogViewModel.BuildIndividualRequest());

            await LoadOrdersAsync();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to create order.";
        }
    }
}
