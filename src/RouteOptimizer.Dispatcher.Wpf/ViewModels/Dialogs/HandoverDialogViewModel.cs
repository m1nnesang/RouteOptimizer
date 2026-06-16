using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

namespace RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

public partial class HandoverDialogViewModel : ObservableObject
{
    private readonly IApiHttpClient _apiHttpClient;

    public HandoverDialogViewModel(IApiHttpClient apiHttpClient, IEnumerable<RouteStop> pendingStops)
    {
        _apiHttpClient = apiHttpClient;

        foreach (var stop in pendingStops)
        {
            var assignment = PendingStopAssignment.From(stop);
            assignment.PropertyChanged += OnStopAssignmentChanged;
            PendingStops.Add(assignment);
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial bool IsTransferAll { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial bool IsDistribute { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial bool IsReturnToPool { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<ShiftListItem> AvailableShifts { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial ShiftListItem? TargetShift { get; set; }

    public ObservableCollection<PendingStopAssignment> PendingStops { get; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool CanConfirm =>
        IsReturnToPool
        || (IsTransferAll && TargetShift is not null)
        || (IsDistribute && PendingStops.Count > 0 && PendingStops.All(s => s.SelectedShift is not null));

    [RelayCommand]
    private async Task LoadShiftsAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _apiHttpClient.GetAsync<PagedResult<ShiftListItem>>("api/drivers/shifts?pageSize=200");
            AvailableShifts = new ObservableCollection<ShiftListItem>(result?.Items ?? []);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Failed to load shifts. Please check your connection.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public HandoverRouteRequest BuildRequest()
    {
        if (IsReturnToPool)
            return new HandoverRouteRequest(HandoverType.ReturnToPool, null, null);

        if (IsTransferAll)
            return new HandoverRouteRequest(HandoverType.TransferAll, TargetShift?.Id, null);

        var assignments = PendingStops
            .Where(s => s.SelectedShift is not null)
            .GroupBy(s => s.SelectedShift!.Id)
            .Select(g => new ShiftStopsAssignment(g.Key, g.Select(s => s.StopId).ToList()))
            .ToList();

        return new HandoverRouteRequest(HandoverType.Distribute, null, assignments);
    }

    private void OnStopAssignmentChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PendingStopAssignment.SelectedShift))
            OnPropertyChanged(nameof(CanConfirm));
    }
}

public partial class PendingStopAssignment : ObservableObject
{
    [ObservableProperty]
    public partial ShiftListItem? SelectedShift { get; set; }

    public Guid StopId { get; init; }
    public string City { get; init; } = string.Empty;
    public string Street { get; init; } = string.Empty;
    public int OrdersCount { get; init; }

    public static PendingStopAssignment From(RouteStop stop) => new()
    {
        StopId = stop.Id,
        City = stop.City,
        Street = stop.Street,
        OrdersCount = stop.OrdersCount
    };
}
