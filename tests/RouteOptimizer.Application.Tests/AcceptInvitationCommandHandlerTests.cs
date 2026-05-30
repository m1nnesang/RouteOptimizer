using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Auth.AcceptInvitation;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Tests;

public class AcceptInvitationCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IUserInvitationRepository> _invitationRepo = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly AcceptInvitationCommandHandler _handler;

    public AcceptInvitationCommandHandlerTests()
    {
        _handler = new AcceptInvitationCommandHandler(
            _userRepo.Object,
            _invitationRepo.Object,
            _passwordHasher.Object,
            _unitOfWork.Object);
    }

    #region Failure Cases

    [Fact]
    public async Task Handle_InvitationNotFound_ReturnsFailure()
    {
        _invitationRepo.Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInvitation?)null);

        var command = new AcceptInvitationCommand("nonexistent-token", "NewPassword1!");

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid or expired invitation");
    }

    [Fact]
    public async Task Handle_InvitationIsUsed_ReturnsFailure()
    {
        var invitation = UserInvitation.Create(Guid.NewGuid(), TimeSpan.FromDays(7));
        invitation.Use();

        _invitationRepo.Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInvitation?)invitation);

        var command = new AcceptInvitationCommand(invitation.Token, "NewPassword1!");

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid or expired invitation");
    }

    [Fact]
    public async Task Handle_InvitationIsExpired_ReturnsFailure()
    {
        var invitation = UserInvitation.Create(Guid.NewGuid(), TimeSpan.FromSeconds(-1));

        _invitationRepo.Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInvitation?)invitation);

        var command = new AcceptInvitationCommand(invitation.Token, "NewPassword1!");

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid or expired invitation");
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        var invitation = UserInvitation.Create(Guid.NewGuid(), TimeSpan.FromDays(7));

        _invitationRepo.Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInvitation?)invitation);
        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new AcceptInvitationCommand(invitation.Token, "NewPassword1!");

        var result = await _handler.Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("User not found");
    }

    #endregion

    #region Happy Path

    [Fact]
    public async Task Handle_ValidInvitation_SetsPasswordAndReturnsSuccess()
    {
        var user = CreateActiveUser();
        var invitation = UserInvitation.Create(user.Id, TimeSpan.FromDays(7));

        _invitationRepo.Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInvitation?)invitation);
        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)user);
        _passwordHasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("new-hashed-password");
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new AcceptInvitationCommand(invitation.Token, "NewPassword1!");

        var result = await _handler.Handle(command, default);

        result.IsSuccess.Should().BeTrue();
        invitation.IsUsed.Should().BeTrue();
        user.PasswordHash.Should().Be("new-hashed-password");
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    private User CreateActiveUser()
    {
        var phone = PhoneNumber.Create("+48601234567").Value!;
        return User.Create("test@test.com", "Jan", "Kowalski", "hashedpassword", phone, UserRole.Dispatcher, Guid.NewGuid(),
            null).Value!;
    }
}
