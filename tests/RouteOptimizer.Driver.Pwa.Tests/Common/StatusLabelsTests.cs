using FluentAssertions;
using RouteOptimizer.Driver.Pwa.Common;

namespace RouteOptimizer.Driver.Pwa.Tests.Common;

public class StatusLabelsTests
{
    [Theory]
    [InlineData("Draft", "Черновик")]
    [InlineData("InProgress", "В пути")]
    [InlineData("Completed", "Завершён")]
    [InlineData("Cancelled", "Отменён")]
    public void Route_KnownStatus_ReturnsRussianLabel(string status, string expected) =>
        StatusLabels.Route(status).Should().Be(expected);

    [Theory]
    [InlineData("Pending", "Ожидает")]
    [InlineData("PartiallyCompleted", "Частично")]
    [InlineData("Failed", "Неудача")]
    public void Stop_KnownStatus_ReturnsRussianLabel(string status, string expected) =>
        StatusLabels.Stop(status).Should().Be(expected);

    [Theory]
    [InlineData("InTransit", "В пути")]
    [InlineData("Delivered", "Доставлен")]
    [InlineData("Failed", "Не доставлен")]
    public void Order_KnownStatus_ReturnsRussianLabel(string status, string expected) =>
        StatusLabels.Order(status).Should().Be(expected);

    [Theory]
    [InlineData("Delivered", "is-done")]
    [InlineData("Failed", "is-failed")]
    [InlineData("Cancelled", "is-skipped")]
    [InlineData("InTransit", "is-active")]
    public void OrderCss_MapsStatusToCssClass(string status, string expected) =>
        StatusLabels.OrderCss(status).Should().Be(expected);

    [Theory]
    [InlineData("Completed", "is-done")]
    [InlineData("InProgress", "is-active")]
    [InlineData("PartiallyCompleted", "is-partial")]
    [InlineData("Pending", "is-pending")]
    public void Css_MapsStatusToCssClass(string status, string expected) =>
        StatusLabels.Css(status).Should().Be(expected);

    [Fact]
    public void Route_UnknownStatus_ReturnsRawValue() =>
        StatusLabels.Route("SomethingElse").Should().Be("SomethingElse");

    [Fact]
    public void Css_UnknownStatus_FallsBackToPending() =>
        StatusLabels.Css("Whatever").Should().Be("is-pending");
}
