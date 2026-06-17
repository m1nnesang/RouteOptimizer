using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using RouteOptimizer.API.Services;

namespace RouteOptimizer.API.Tests.Services;

public class CurrentUserTests
{
    private static CurrentUser Create(HttpContext? context)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(context);
        return new CurrentUser(accessor.Object);
    }

    private static HttpContext ContextWith(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "test");
        return new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
    }

    [Fact]
    public void UserId_ReturnsParsedClaim()
    {
        var id = Guid.NewGuid();
        var sut = Create(ContextWith(new Claim(ClaimTypes.NameIdentifier, id.ToString())));

        sut.UserId.Should().Be(id);
    }

    [Fact]
    public void UserId_MissingClaim_Throws()
    {
        var sut = Create(ContextWith());

        var act = () => sut.UserId;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UserId_NoHttpContext_Throws()
    {
        var sut = Create(null);

        var act = () => sut.UserId;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void WarehouseId_ReturnsParsedClaim()
    {
        var warehouseId = Guid.NewGuid();
        var sut = Create(ContextWith(new Claim("warehouse_id", warehouseId.ToString())));

        sut.WarehouseId.Should().Be(warehouseId);
    }

    [Fact]
    public void WarehouseId_MissingClaim_ReturnsNull()
    {
        var sut = Create(ContextWith());

        sut.WarehouseId.Should().BeNull();
    }
}
