using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Drivers.InviteDriver;

public record InviteDriverCommand(
    string Email,
    string FirstName,
    string LastName,
    string PhoneNumber,
    Guid WarehouseId,
    string LicenseCategory) : ICommand<Result>;
