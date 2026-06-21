using FluentAssertions;
using RouteOptimizer.Driver.Pwa.Common;

namespace RouteOptimizer.Driver.Pwa.Tests.Common;

public class FailureReasonsTests
{
    [Fact]
    public void All_ContainsEveryReason() =>
        FailureReasons.All.Select(r => r.Value).Should().BeEquivalentTo(
            "IncorrectAddress", "FirmClosed", "CustomerNotAtHome",
            "CustomerRefuseDelivery", "VehicleProblem", "Other");

    [Fact]
    public void All_EndsWithOtherAsFallback() =>
        FailureReasons.All.Last().Value.Should().Be("Other");

    [Fact]
    public void All_LabelsAreNonEmpty() =>
        FailureReasons.All.Should().OnlyContain(r => !string.IsNullOrWhiteSpace(r.Label));

    [Fact]
    public void All_ValuesAreUnique() =>
        FailureReasons.All.Select(r => r.Value).Should().OnlyHaveUniqueItems();
}
