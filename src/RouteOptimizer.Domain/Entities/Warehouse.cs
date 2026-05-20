using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Domain.Entities;

public class Warehouse : AggregateRoot<Guid>
{
    private Warehouse() : base(default)
    {
        Name = null!;
        Address = null!;
        Location = null!;
    } // EF Core

    private Warehouse(Guid id, string name, Address address, GeoCoordinate location) : base(id)
    {
        (Name, Address, Location) = (name, address, location);
    }

    public string Name { get; }
    public Address Address { get; }
    public GeoCoordinate Location { get; }

    public static Result<Warehouse> Create(string name, Address address, GeoCoordinate location)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Warehouse>.Failure("Name cannot be empty");


        return Result<Warehouse>.Success(new Warehouse(Guid.NewGuid(), name, address, location));
    }
}