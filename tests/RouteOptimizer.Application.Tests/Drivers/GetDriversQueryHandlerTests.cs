using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Common;
using RouteOptimizer.Application.Drivers.GetDrivers;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Drivers;

public class GetDriversQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly GetDriversQueryHandler _handler;

    public GetDriversQueryHandlerTests()
    {
        _handler = new GetDriversQueryHandler(_userRepository.Object, _currentUser.Object);
    }

    [Fact]
    public async Task Handle_NoDrivers_ReturnsEmptyList()
    {
        _userRepository
            .Setup(x => x.GetAllDriversAsync(It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<User>(), 0));

        var result = await _handler.Handle(new GetDriversQuery(), default);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DriversExist_ReturnsMappedDtos()
    {
        var driver = CreateDriver();
        _userRepository
            .Setup(x => x.GetAllDriversAsync(It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<User> { driver }, 1));

        var result = await _handler.Handle(new GetDriversQuery(), default);

        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(driver.Id);
        result.Items[0].Email.Should().Be(driver.Email);
    }

    [Fact]
    public async Task Handle_ScopesQueryToCurrentUserWarehouse()
    {
        var warehouseId = Guid.NewGuid();
        _currentUser.Setup(x => x.WarehouseId).Returns(warehouseId);
        _userRepository
            .Setup(x => x.GetAllDriversAsync(It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<User>(), 0));

        await _handler.Handle(new GetDriversQuery(), default);

        _userRepository.Verify(
            x => x.GetAllDriversAsync(warehouseId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static User CreateDriver()
    {
        var phone = PhoneNumber.Create("+48601234567").Value!;
        return User.Create("driver@test.com", "Jan", "Kowalski", "hashed", phone,
            UserRole.Driver, Guid.NewGuid(), new DriverProfile(LicenseCategory.B)).Value!;
    }
}
