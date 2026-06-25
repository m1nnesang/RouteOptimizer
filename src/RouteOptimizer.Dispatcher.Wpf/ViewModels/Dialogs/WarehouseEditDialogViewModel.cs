using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;

namespace RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

public partial class WarehouseEditDialogViewModel : ObservableObject
{
    private const int SuggestDebounceMs = 300;
    private const int MinSuggestLength = 3;

    private readonly IApiHttpClient? _apiHttpClient;
    private CancellationTokenSource? _suggestCts;
    private bool _suppressSuggest;

    public WarehouseEditDialogViewModel(WarehouseListItem? existing = null,
        IApiHttpClient? apiHttpClient = null)
    {
        _apiHttpClient = apiHttpClient;
        IsEditMode = existing is not null;
        if (existing is not null)
        {
            _suppressSuggest = true;
            Name = existing.Name;
            City = existing.City;
            Street = existing.Street;
            _suppressSuggest = false;
        }
    }

    public bool IsEditMode { get; }

    public ObservableCollection<AddressSuggestion> AddressSuggestions { get; } = [];

    [ObservableProperty]
    public partial bool IsSuggestionsOpen { get; set; }

    [ObservableProperty]
    public partial AddressSuggestion? SelectedSuggestion { get; set; }

    partial void OnStreetChanged(string value)
    {
        if (_suppressSuggest)
            return;

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

    partial void OnSelectedSuggestionChanged(AddressSuggestion? value)
    {
        if (value is null)
            return;

        _suppressSuggest = true;
        Street = value.Street;
        City = value.City;
        PostalCode = value.Postcode;
        Country = value.Country;
        _suppressSuggest = false;

        AddressSuggestions.Clear();
        IsSuggestionsOpen = false;
    }

    public string Title => IsEditMode ? "Edit Warehouse" : "Create Warehouse";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial string City { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial string Street { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PostalCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Country { get; set; } = string.Empty;

    public bool CanConfirm =>
        !string.IsNullOrWhiteSpace(Name) &&
        !string.IsNullOrWhiteSpace(City) &&
        !string.IsNullOrWhiteSpace(Street);

    public CreateWarehouseRequest BuildCreateRequest() =>
        new(Name, City, Street, PostalCode, Country);

    public UpdateWarehouseRequest BuildUpdateRequest() =>
        new(Name, Street, City, PostalCode, Country);
}
