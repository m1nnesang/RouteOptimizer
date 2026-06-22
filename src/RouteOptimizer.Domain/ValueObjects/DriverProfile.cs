using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Domain.ValueObjects;

public class DriverProfile : ValueObject
{
    private DriverProfile()
    {
    }

    public DriverProfile(LicenseCategory highestLicenseCategory)
    {
        HighestLicenseCategory = highestLicenseCategory;
    }

    public LicenseCategory HighestLicenseCategory { get; }

    public bool CanDrive(LicenseCategory required)
    {
        return HighestLicenseCategory >= required;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return HighestLicenseCategory;
    }
}