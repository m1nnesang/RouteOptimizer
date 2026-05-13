using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Domain.ValueObjects;

public class DriverProfile : ValueObject
{
    public LicenseCategory HighestLicenseCategory { get; }
    
    public DriverProfile (LicenseCategory highestLicenseCategory)
    {
        HighestLicenseCategory = highestLicenseCategory;
    }
    
    public bool CanDrive(LicenseCategory required) => HighestLicenseCategory >= required;
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return HighestLicenseCategory;
    }
}