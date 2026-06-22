using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Domain.ValueObjects;

public class Address : ValueObject
{
    private Address()
    {
        Street = null!;
        City = null!;
        PostalCode = null!;
        Country = null!;
    }

    private Address(string street, string city, string postalCode, string country, string? apartment)
    {
        Street = street;
        City = city;
        PostalCode = postalCode;
        Country = country;
        Apartment = apartment;
    }

    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string Country { get; }
    public string? Apartment { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return PostalCode;
        yield return Country;
        yield return Apartment;
    }

    public static Result<Address> Create(string street, string city, string postalCode, string country,
        string? apartment = null)
    {
        if (string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(city) ||
            string.IsNullOrWhiteSpace(postalCode) ||
            string.IsNullOrWhiteSpace(country)) return Result<Address>.Failure("All fields are required");

        var normalizedApartment = string.IsNullOrWhiteSpace(apartment) ? null : apartment.Trim();
        return Result<Address>.Success(new Address(street, city, postalCode, country, normalizedApartment));
    }
}
