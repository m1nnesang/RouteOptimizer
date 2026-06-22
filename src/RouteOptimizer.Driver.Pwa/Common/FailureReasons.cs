namespace RouteOptimizer.Driver.Pwa.Common;

public static class FailureReasons
{
    public static readonly IReadOnlyList<(string Value, string Label)> All =
    [
        ("IncorrectAddress", "Incorrect address"),
        ("FirmClosed", "Business closed"),
        ("CustomerNotAtHome", "Customer not at home"),
        ("CustomerRefuseDelivery", "Customer refused"),
        ("VehicleProblem", "Vehicle problem"),
        ("Other", "Other"),
    ];
}
