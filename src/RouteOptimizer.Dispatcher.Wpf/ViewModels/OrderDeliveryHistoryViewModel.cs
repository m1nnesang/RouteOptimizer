using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

namespace RouteOptimizer.Dispatcher.Wpf.ViewModels;

public partial class OrderDeliveryHistoryViewModel : ObservableObject
{
    private readonly IApiHttpClient _apiHttpClient;

    public OrderDeliveryHistoryViewModel(IApiHttpClient apiHttpClient)
    {
        _apiHttpClient = apiHttpClient;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial ObservableCollection<DeliveryAttemptItem> Attempts { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool IsEmpty => !IsLoading && !HasError && Attempts.Count == 0;

    public async Task LoadAsync(Guid orderId, CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        Attempts = [];
        try
        {
            var attempts = await _apiHttpClient.GetAsync<List<DeliveryAttemptItem>>(
                $"api/orders/{orderId}/delivery-attempts", ct);
            Attempts = new ObservableCollection<DeliveryAttemptItem>(attempts ?? []);

            await ResolvePhotoUrlsAsync(ct);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to load delivery history.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ResolvePhotoUrlsAsync(CancellationToken ct)
    {
        foreach (var attempt in Attempts.Where(a => a.HasPhoto))
        {
            try
            {
                var url = await _apiHttpClient.GetAsync<string>(
                    $"api/delivery-photos/{Uri.EscapeDataString(attempt.PhotoKey!)}", ct);
                attempt.PhotoUrl = url;
            }
            catch (HttpRequestException)
            {
            }
        }
    }
}
