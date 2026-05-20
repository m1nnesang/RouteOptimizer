namespace RouteOptimizer.Application.Orders.GetOrderById;

public record OrderDto(
    Guid Id,
    string City,
    string Street,
    string PostalCode,
    string Country,
    double Latitude,
    double Longitude,
    TimeOnly? Start,
    TimeOnly? End,
    string? CompanyName,
    string? ContactPerson,
    string? CustomerName,
    string PhoneNumber,
    decimal Weight,
    decimal Volume,
    string CargoType,
    string? Notes);
