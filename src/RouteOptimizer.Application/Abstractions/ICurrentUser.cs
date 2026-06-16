namespace RouteOptimizer.Application.Abstractions;

public interface ICurrentUser
{
    Guid UserId { get; }
    Guid? WarehouseId { get; }
}
