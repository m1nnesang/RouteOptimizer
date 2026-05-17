using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Orders.CreateBusinessOrder;

public record CreateBusinessOrderCommand(
    Guid WarehouseId,
    string CompanyName,
    string ContactPerson,
    TimeOnly Start,
    TimeOnly End,
    string Street,
    string City,
    string Postcode,
    string Country,
    string PhoneNumber,
    decimal Weight,
    decimal Volume,
    string CargoType,
    string? Notes,
    string WindowStrictness) : IRequest<Result<Guid>>;