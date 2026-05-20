namespace RouteOptimizer.Domain.Enums;

public enum OrderStatus
{
    Created,
    AssignedToRoute,
    InTransit,
    Delivered,
    Failed,
    Cancelled
}