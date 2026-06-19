using System.Net.Http;
using FluentAssertions;
using Moq;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.ViewModels;

namespace RouteOptimizer.Dispatcher.Wpf.Tests.ViewModels;

public class LoginViewModelTests
{
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IRouteHubService> _routeHubService = new();

    private LoginViewModel CreateViewModel() => new(_authService.Object, _routeHubService.Object);

    [Fact]
    public async Task LoginAsync_Success_RaisesLoginSucceededAndClearsError()
    {
        _authService.Setup(a => a.LoginAsync("user@test.com", "pass", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var vm = CreateViewModel();
        vm.Email = "user@test.com";
        vm.Password = "pass";
        var raised = false;
        vm.LoginSucceeded += () => raised = true;

        await vm.LoginAsync(default);

        raised.Should().BeTrue();
        vm.ErrorMessage.Should().BeEmpty();
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_Failure_SetsErrorMessage()
    {
        _authService.Setup(a => a.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var vm = CreateViewModel();
        var raised = false;
        vm.LoginSucceeded += () => raised = true;

        await vm.LoginAsync(default);

        raised.Should().BeFalse();
        vm.ErrorMessage.Should().Be("Login failed");
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_NetworkError_SetsConnectionMessage()
    {
        _authService.Setup(a => a.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("boom"));
        var vm = CreateViewModel();

        await vm.LoginAsync(default);

        vm.ErrorMessage.Should().Be("Network error. Please check your connection.");
        vm.IsLoading.Should().BeFalse();
    }
}
