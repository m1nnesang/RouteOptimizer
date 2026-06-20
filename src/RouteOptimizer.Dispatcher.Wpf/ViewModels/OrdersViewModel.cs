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
    private const string AllFilter = "All";

    private static readonly string[] OrderStatuses =
    [
        "Created", "AssignedToRoute", "InTransit", "Delivered", "Failed", "Cancelled"
    ];

    private readonly IApiHttpClient _apiHttpClient;
    private readonly IDialogService _dialogService;
    private List<OrderListItem> _allOrders = [];

    public OrdersViewModel(IApiHttpClient apiHttpClient, IDialogService dialogService)
    {
        _apiHttpClient = apiHttpClient;
        _dialogService = dialogService;
        DeliveryHistory = new OrderDeliveryHistoryViewModel(apiHttpClient);
        StatusFilters = new ObservableCollection<string>(
            new[] { AllFilter }.Concat(OrderStatuses));
        _ = LoadOrdersAsync();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial ObservableCollection<OrderListItem> Orders { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<string> StatusFilters { get; set; } = [];

    [ObservableProperty]
    public partial string SelectedStatusFilter { get; set; } = AllFilter;

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

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

    partial void OnSelectedStatusFilterChanged(string value) => _ = LoadOrdersAsync();

    partial void OnSearchTextChanged(string value) => ApplyFilter();

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
            if (!string.IsNullOrEmpty(SelectedStatusFilter) && SelectedStatusFilter != AllFilter)
                url += $"&status={SelectedStatusFilter}";
            if (SelectedDate is not null)
                url += $"&date={SelectedDate.Value:yyyy-MM-dd}";

            var result = await _apiHttpClient.GetAsync<PagedResult<OrderListItem>>(url);
            _allOrders = (result?.Items ?? []).ToList();
            ApplyFilter();
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

    private void ApplyFilter()
    {
        var query = SearchText?.Trim();
        IEnumerable<OrderListItem> items = _allOrders;

        if (!string.IsNullOrEmpty(query))
            items = items.Where(o => MatchesSearch(o, query));

        Orders = new ObservableCollection<OrderListItem>(items);
    }

    private static bool MatchesSearch(OrderListItem order, string query) =>
        Contains(order.Status, query)
        || Contains(order.OrderType, query)
        || Contains(order.City, query)
        || Contains(order.Street, query)
        || Contains(order.CargoType, query)
        || Contains(order.PhoneNumber, query)
        || Contains(order.CompanyName, query)
        || Contains(order.CustomerName, query);

    private static bool Contains(string? value, string query) =>
        value is not null && value.Contains(query, StringComparison.OrdinalIgnoreCase);

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
