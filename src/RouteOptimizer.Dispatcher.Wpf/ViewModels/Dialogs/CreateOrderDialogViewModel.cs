using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

namespace RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

public enum OrderKind
{
    Business,
    Individual
}

public partial class CreateOrderDialogViewModel : ObservableObject
{
    private const int SuggestDebounceMs = 300;
    private const int MinSuggestLength = 3;

    private static readonly string[] CargoTypes = ["General", "Refrigerated", "Fragile", "Hazardous"];
    private static readonly string[] StrictnessOptionsSource = ["Soft", "Strict"];

    private readonly IApiHttpClient? _apiHttpClient;
    private CancellationTokenSource? _suggestCts;
    private bool _suppressSuggest;
    private double? _selectedLatitude;
    private double? _selectedLongitude;

    public CreateOrderDialogViewModel(IReadOnlyList<WarehouseListItem> warehouses,
        IApiHttpClient? apiHttpClient = null)
    {
        _apiHttpClient = apiHttpClient;
        Warehouses = new ObservableCollection<WarehouseListItem>(warehouses);
        SelectedWarehouse = Warehouses.FirstOrDefault();
        CargoTypeOptions = new ObservableCollection<string>(CargoTypes);
        StrictnessOptions = new ObservableCollection<string>(StrictnessOptionsSource);
    }

    public ObservableCollection<AddressSuggestion> AddressSuggestions { get; } = [];

    [ObservableProperty]
    public partial bool IsSuggestionsOpen { get; set; }

    partial void OnStreetChanged(string value)
    {
        if (_suppressSuggest)
            return;

        _selectedLatitude = null;
        _selectedLongitude = null;
        _ = SearchSuggestionsAsync(value);
    }

    private async Task SearchSuggestionsAsync(string query)
    {
        if (_apiHttpClient is null)
            return;

        _suggestCts?.Cancel();
        var cts = new CancellationTokenSource();
        _suggestCts = cts;

        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < MinSuggestLength)
        {
            AddressSuggestions.Clear();
            IsSuggestionsOpen = false;
            return;
        }

        try
        {
            await Task.Delay(SuggestDebounceMs, cts.Token);

            var url = $"api/geocoding/autocomplete?q={Uri.EscapeDataString(query.Trim())}";
            var results = await _apiHttpClient.GetAsync<List<AddressSuggestion>>(url, cts.Token);

            if (cts.Token.IsCancellationRequested)
                return;

            AddressSuggestions.Clear();
            foreach (var suggestion in results ?? [])
                AddressSuggestions.Add(suggestion);

            IsSuggestionsOpen = AddressSuggestions.Count > 0;
        }
        catch (OperationCanceledException)
        {
        }
        catch (HttpRequestException)
        {
            AddressSuggestions.Clear();
            IsSuggestionsOpen = false;
        }
    }

    [ObservableProperty]
    public partial AddressSuggestion? SelectedSuggestion { get; set; }

    partial void OnSelectedSuggestionChanged(AddressSuggestion? value)
    {
        if (value is null)
            return;

        var suggestion = value;
        _suppressSuggest = true;
        Street = suggestion.Street;
        City = suggestion.City;
        Postcode = suggestion.Postcode;
        Country = suggestion.Country;
        _suppressSuggest = false;

        _selectedLatitude = suggestion.Latitude;
        _selectedLongitude = suggestion.Longitude;

        AddressSuggestions.Clear();
        IsSuggestionsOpen = false;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTypeSelected))]
    [NotifyPropertyChangedFor(nameof(IsTypeNotSelected))]
    [NotifyPropertyChangedFor(nameof(IsBusiness))]
    [NotifyPropertyChangedFor(nameof(IsIndividual))]
    [NotifyPropertyChangedFor(nameof(Title))]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial OrderKind? OrderType { get; set; }

    public bool IsTypeSelected => OrderType is not null;
    public bool IsTypeNotSelected => OrderType is null;
    public bool IsBusiness => OrderType == OrderKind.Business;
    public bool IsIndividual => OrderType == OrderKind.Individual;

    public string Title => OrderType switch
    {
        OrderKind.Business => "New business order",
        OrderKind.Individual => "New individual order",
        _ => "New order"
    };

    [RelayCommand]
    private void SelectBusiness() => OrderType = OrderKind.Business;

    [RelayCommand]
    private void SelectIndividual() => OrderType = OrderKind.Individual;

    [RelayCommand]
    private void Back() => OrderType = null;

    public ObservableCollection<WarehouseListItem> Warehouses { get; }
    public ObservableCollection<string> CargoTypeOptions { get; }
    public ObservableCollection<string> StrictnessOptions { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial WarehouseListItem? SelectedWarehouse { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial string SelectedCargoType { get; set; } = "General";

    [ObservableProperty]
    public partial string SelectedStrictness { get; set; } = "Soft";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial string Street { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Apartment { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial string City { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial string Postcode { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial string Country { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial string PhoneNumber { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial string WeightText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial string VolumeText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial string StartText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial string EndText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial string CompanyName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial string ContactPerson { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial string CustomerName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool AllowLeaveAtDoor { get; set; }

    public bool CanConfirm
    {
        get
        {
            if (OrderType is null || SelectedWarehouse is null)
                return false;

            if (string.IsNullOrWhiteSpace(Street) ||
                string.IsNullOrWhiteSpace(City) ||
                string.IsNullOrWhiteSpace(Postcode) ||
                string.IsNullOrWhiteSpace(Country) ||
                string.IsNullOrWhiteSpace(PhoneNumber) ||
                string.IsNullOrWhiteSpace(SelectedCargoType))
                return false;

            if (!TryParseDecimal(WeightText, out var weight) || weight <= 0 ||
                !TryParseDecimal(VolumeText, out var volume) || volume <= 0)
                return false;

            if (IsBusiness)
            {
                if (string.IsNullOrWhiteSpace(CompanyName) || string.IsNullOrWhiteSpace(ContactPerson))
                    return false;

                if (!TryParseTime(StartText, out var start) || !TryParseTime(EndText, out var end) || end <= start)
                    return false;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(CustomerName))
                    return false;
            }

            return true;
        }
    }

    public CreateBusinessOrderRequest BuildBusinessRequest()
    {
        TryParseTime(StartText, out var start);
        TryParseTime(EndText, out var end);
        TryParseDecimal(WeightText, out var weight);
        TryParseDecimal(VolumeText, out var volume);

        return new CreateBusinessOrderRequest(
            SelectedWarehouse!.Id,
            CompanyName.Trim(),
            ContactPerson.Trim(),
            start,
            end,
            Street.Trim(),
            City.Trim(),
            Postcode.Trim(),
            Country.Trim(),
            PhoneNumber.Trim(),
            weight,
            volume,
            SelectedCargoType,
            NullIfEmpty(Notes),
            SelectedStrictness,
            NullIfEmpty(Apartment),
            _selectedLatitude,
            _selectedLongitude);
    }

    public CreateIndividualOrderRequest BuildIndividualRequest()
    {
        var hasStart = TryParseTime(StartText, out var start);
        var hasEnd = TryParseTime(EndText, out var end);
        TryParseDecimal(WeightText, out var weight);
        TryParseDecimal(VolumeText, out var volume);

        TimeOnly? startValue = hasStart ? start : null;
        TimeOnly? endValue = hasEnd ? end : null;
        string? strictness = startValue is null && endValue is null ? null : SelectedStrictness;

        return new CreateIndividualOrderRequest(
            SelectedWarehouse!.Id,
            Street.Trim(),
            City.Trim(),
            Postcode.Trim(),
            Country.Trim(),
            CustomerName.Trim(),
            PhoneNumber.Trim(),
            weight,
            volume,
            SelectedCargoType,
            AllowLeaveAtDoor,
            NullIfEmpty(Notes),
            startValue,
            endValue,
            strictness,
            NullIfEmpty(Apartment),
            _selectedLatitude,
            _selectedLongitude);
    }

    private static bool TryParseDecimal(string text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);

    private static bool TryParseTime(string text, out TimeOnly value) =>
        TimeOnly.TryParse(text, CultureInfo.InvariantCulture, out value);

    private static string? NullIfEmpty(string text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
