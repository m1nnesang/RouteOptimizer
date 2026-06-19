namespace RouteOptimizer.Application.Geocoding;

public record AddressSuggestion(
    string Label,
    string Street,
    string City,
    string Postcode,
    string Country,
    double Latitude,
    double Longitude);
