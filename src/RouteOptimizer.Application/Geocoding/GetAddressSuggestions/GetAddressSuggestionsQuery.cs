using RouteOptimizer.Application.Abstractions;

namespace RouteOptimizer.Application.Geocoding.GetAddressSuggestions;

public record GetAddressSuggestionsQuery(string Query) : IQuery<IReadOnlyList<AddressSuggestion>>;
