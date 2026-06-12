using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Application.Users.DeactivateUser;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests.Users;

public class DeactivateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly DeactivateUserCommandHandler _handler;

    public DeactivateUserCommandHandlerTests()
    {
        _handler = new DeactivateUserCommandHandler(_userRepo.Object);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => _handler.Handle(new DeactivateUserCommand(Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_AlreadyDeactivatedUser_ReturnsFailure()
    {
        var user = CreateUser();
        user.Deactivate();
        _userRepo.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(new DeactivateUserCommand(user.Id), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ActiveUser_DeactivatesAndReturnsSuccess()
    {
        var user = CreateUser();
        _userRepo.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(new DeactivateUserCommand(user.Id), default);

        result.IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeFalse();
    }

    private static User CreateUser()
    {
        var phone = PhoneNumber.Create("+48601234567").Value!;
        var profile = new DriverProfile(LicenseCategory.B);

        return User.Create("jan@test.com", "Jan", "Kowalski", "hash", phone, UserRole.Driver,
            Guid.NewGuid(), profile).Value!;
    }
}
