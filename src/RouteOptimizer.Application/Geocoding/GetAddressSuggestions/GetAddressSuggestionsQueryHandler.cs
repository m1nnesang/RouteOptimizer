using MediatR;
using RouteOptimizer.Application.Abstractions;

namespace RouteOptimizer.Application.Geocoding.GetAddressSuggestions;

public class GetAddressSuggestionsQueryHandler
    : IRequestHandler<GetAddressSuggestionsQuery, IReadOnlyList<AddressSuggestion>>
{
    private readonly IAddressAutocompleteService _autocompleteService;

    public GetAddressSuggestionsQueryHandler(IAddressAutocompleteService autocompleteService)
    {
        _autocompleteService = autocompleteService;
    }

    public Task<IReadOnlyList<AddressSuggestion>> Handle(GetAddressSuggestionsQuery request, CancellationToken ct)
    {
        return _autocompleteService.SuggestAsync(request.Query, ct);
    }
}
