namespace RouteOptimizer.Application.Drivers.GetDrivers;

public record DriverListItemDto(Guid Id, string FirstName, string LastName,string Email, string PhoneNumber, bool IsActive, string? LicenseCategory);
