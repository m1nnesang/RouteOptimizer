namespace RouteOptimizer.Application.Orders.GetOrders;

public record OrderListItemDto(Guid Id,string City ,string Street, string Status, string OrderType,string CargoType ,DateTime CreatedAt,decimal Weight, string PhoneNumber, string? CompanyName, string? CustomerName);
