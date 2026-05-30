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
    } // EF Core

    private Address(string street, string city, string postalCode, string country)
    {
        Street = street;
        City = city;
        PostalCode = postalCode;
        Country = country;
    }

    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string Country { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return PostalCode;
        yield return Country;
    }

    public static Result<Address> Create(string street, string city, string postalCode, string country)
    {
        if (string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(city) ||
            string.IsNullOrWhiteSpace(postalCode) ||
            string.IsNullOrWhiteSpace(country)) return Result<Address>.Failure("All fields are required");

        return Result<Address>.Success(new Address(street, city, postalCode, country));
    }
}
