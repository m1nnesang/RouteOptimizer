namespace RouteOptimizer.Dispatcher.Wpf.Models;

public class DriverListItem
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? LicenseCategory { get; set; }

    public string DisplayName => $"{FirstName} {LastName}".Trim();
}
