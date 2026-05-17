using MediatR;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Application.Orders.CreateIndividualOrder;

public record CreateIndividualOrderCommand(Guid WarehouseId, string Street, string City, string Postcode, string Country, string CustomerName, string PhoneNumber,
    decimal Weight, decimal Volume, string CargoType, bool AllowLeaveAtDoor, string? Notes, TimeOnly? Start ,TimeOnly? End, string? WindowStrictness
)
    : IRequest<Result<Guid>>;