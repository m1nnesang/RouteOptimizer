using FluentAssertions;
using Moq;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.ViewModels;

namespace RouteOptimizer.Dispatcher.Wpf.Tests.ViewModels;

public class LoginViewModelResetTests
{
    private readonly Mock<IAuthService> _auth = new();
    private readonly Mock<IRouteHubService> _hub = new();
    private readonly LoginViewModel _vm;

    public LoginViewModelResetTests()
    {
        _vm = new LoginViewModel(_auth.Object, _hub.Object);
    }

    [Fact]
    public void ShowReset_EntersResetMode()
    {
        _vm.ShowResetCommand.Execute(null);

        _vm.IsResetMode.Should().BeTrue();
        _vm.IsLoginMode.Should().BeFalse();
    }

    [Fact]
    public async Task RequestReset_AlwaysShowsNeutralMessage()
    {
        _auth.Setup(a => a.RequestPasswordResetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _vm.ResetEmail = "driver@test.com";

        await _vm.RequestResetCommand.ExecuteAsync(null);

        _vm.ResetMessage.Should().Contain("reset link");
    }

    [Fact]
    public async Task ResetPassword_Success_LeavesResetModeAndConfirms()
    {
        _auth.Setup(a => a.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _vm.ShowResetCommand.Execute(null);
        _vm.ResetToken = "token";
        _vm.ResetNewPassword = "NewPassword1!";

        await _vm.ResetPasswordCommand.ExecuteAsync(null);

        _vm.IsResetMode.Should().BeFalse();
        _vm.ResetMessage.Should().Contain("sign in");
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_StaysInResetMode()
    {
        _auth.Setup(a => a.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _vm.ShowResetCommand.Execute(null);
        _vm.ResetToken = "bad";
        _vm.ResetNewPassword = "NewPassword1!";

        await _vm.ResetPasswordCommand.ExecuteAsync(null);

        _vm.IsResetMode.Should().BeTrue();
        _vm.ResetMessage.Should().Contain("Invalid");
    }
}
