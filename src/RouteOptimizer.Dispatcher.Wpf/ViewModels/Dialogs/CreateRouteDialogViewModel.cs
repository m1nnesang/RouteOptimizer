using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

namespace RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

public partial class CreateRouteDialogViewModel : ObservableObject
{
    private readonly IApiHttpClient _apiHttpClient;

    public CreateRouteDialogViewModel(IApiHttpClient apiHttpClient)
    {
        _apiHttpClient = apiHttpClient;
    }

    [ObservableProperty]
    public partial DateTime SelectedDate { get; set; } = DateTime.Today;

    public DateOnly RouteDate => DateOnly.FromDateTime(SelectedDate);

    [ObservableProperty]
    public partial ObservableCollection<SelectableOrderItem> AvailableOrders { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedOrders))]
    public partial int SelectedCount { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool HasSelectedOrders => SelectedCount > 0;

    public IReadOnlyList<Guid> SelectedOrderIds =>
        AvailableOrders.Where(o => o.IsSelected).Select(o => o.Id).ToList();

    [RelayCommand]
    private async Task LoadOrdersAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _apiHttpClient.GetAsync<PagedResult<OrderListItem>>("api/orders?status=Created&pageSize=100");

            foreach (var item in AvailableOrders)
                item.PropertyChanged -= OnItemPropertyChanged;

            var items = (result?.Items ?? []).Select(SelectableOrderItem.From).ToList();
            foreach (var item in items)
                item.PropertyChanged += OnItemPropertyChanged;

            AvailableOrders = new ObservableCollection<SelectableOrderItem>(items);
            SelectedCount = 0;
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

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectableOrderItem.IsSelected))
            SelectedCount = AvailableOrders.Count(o => o.IsSelected);
    }
}

public partial class SelectableOrderItem : ObservableObject
{
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public Guid Id { get; init; }
    public string OrderType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Street { get; init; } = string.Empty;
    public string CargoType { get; init; } = string.Empty;
    public decimal Weight { get; init; }

    public static SelectableOrderItem From(OrderListItem order) => new()
    {
        Id = order.Id,
        OrderType = order.OrderType,
        Status = order.Status,
        City = order.City,
        Street = order.Street,
        CargoType = order.CargoType,
        Weight = order.Weight
    };
}
