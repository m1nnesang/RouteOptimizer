using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

namespace RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

public partial class InsertUrgentOrderDialogViewModel : ObservableObject
{
    private readonly IApiHttpClient _apiHttpClient;

    public InsertUrgentOrderDialogViewModel(IApiHttpClient apiHttpClient)
    {
        _apiHttpClient = apiHttpClient;
    }

    [ObservableProperty]
    public partial ObservableCollection<OrderListItem> AvailableOrders { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedOrder))]
    public partial OrderListItem? SelectedOrder { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool HasSelectedOrder => SelectedOrder is not null;

    [RelayCommand]
    private async Task LoadOrdersAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _apiHttpClient.GetAsync<PagedResult<OrderListItem>>("api/orders?status=Created&pageSize=200");
            AvailableOrders = new ObservableCollection<OrderListItem>(result?.Items ?? []);
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
}
