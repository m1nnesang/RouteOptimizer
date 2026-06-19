using RouteOptimizer.Application.Geocoding;

namespace RouteOptimizer.Application.Abstractions;

public interface IAddressAutocompleteService
{
    Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(string query, CancellationToken ct);
}
