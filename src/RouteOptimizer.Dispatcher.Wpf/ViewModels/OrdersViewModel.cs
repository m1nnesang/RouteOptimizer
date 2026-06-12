using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

namespace RouteOptimizer.Dispatcher.Wpf.VIewModels;

public partial class OrdersViewModel : ObservableObject
{
    private readonly IApiHttpClient _apiHttpClient;

    public OrdersViewModel(IApiHttpClient apiHttpClient)
    {
        _apiHttpClient = apiHttpClient;
        _ = LoadOrdersAsync();
    }

    [ObservableProperty]
    public partial ObservableCollection<OrderListItem> Orders { get; set; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [RelayCommand]
    private async Task LoadOrdersAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _apiHttpClient.GetAsync<PagedResult<OrderListItem>>("api/orders");
            Orders = new ObservableCollection<OrderListItem>(result?.Items ?? []);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
