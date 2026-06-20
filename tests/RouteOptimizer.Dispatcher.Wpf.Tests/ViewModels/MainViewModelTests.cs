using FluentAssertions;
using Moq;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.ViewModels;

namespace RouteOptimizer.Dispatcher.Wpf.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly Mock<IAuthService> _auth = new();
    private readonly Mock<IApiHttpClient> _api = new();
    private readonly Mock<IDialogService> _dialog = new();
    private readonly Mock<IRouteHubService> _routeHub = new();
    private readonly Mock<ISessionNotifier> _sessionNotifier = new();

    private MainViewModel CreateViewModel()
    {
        _api.Setup(a => a.GetAsync<object>(It.IsAny<string>(), default)).ReturnsAsync((object?)null);
        return new MainViewModel(_auth.Object, _api.Object, _dialog.Object,
            _routeHub.Object, _sessionNotifier.Object);
    }

    [Fact]
    public void Constructor_CurrentViewModelIsOrders()
    {
        var vm = CreateViewModel();
        vm.CurrentViewModel.Should().BeOfType<OrdersViewModel>();
    }

    [Fact]
    public void ShowRoutes_CurrentViewModelIsRoutes()
    {
        var vm = CreateViewModel();
        vm.ShowRoutesCommand.Execute(null);
        vm.CurrentViewModel.Should().BeOfType<RoutesViewModel>();
    }

    [Fact]
    public void ShowDrivers_CurrentViewModelIsDrivers()
    {
        var vm = CreateViewModel();
        vm.ShowDriversCommand.Execute(null);
        vm.CurrentViewModel.Should().BeOfType<DriversViewModel>();
    }

    [Fact]
    public void ShowVehicles_CurrentViewModelIsVehicles()
    {
        var vm = CreateViewModel();
        vm.ShowVehiclesCommand.Execute(null);
        vm.CurrentViewModel.Should().BeOfType<VehiclesViewModel>();
    }

    [Fact]
    public void ShowWarehouses_CurrentViewModelIsWarehouses()
    {
        var vm = CreateViewModel();
        vm.ShowWarehousesCommand.Execute(null);
        vm.CurrentViewModel.Should().BeOfType<WarehousesViewModel>();
    }

    [Fact]
    public void ShowOrders_CurrentViewModelIsOrders()
    {
        var vm = CreateViewModel();
        vm.ShowRoutesCommand.Execute(null);
        vm.ShowOrdersCommand.Execute(null);
        vm.CurrentViewModel.Should().BeOfType<OrdersViewModel>();
    }

    [Fact]
    public void SessionExpired_ShowsBanner()
    {
        var vm = CreateViewModel();

        _sessionNotifier.Raise(s => s.SessionExpired += null);

        vm.IsSessionExpired.Should().BeTrue();
    }

    [Fact]
    public async Task ReLogin_HidesBanner_LogsOut_AndRaisesLogoutRequested()
    {
        var vm = CreateViewModel();
        _sessionNotifier.Raise(s => s.SessionExpired += null);
        var logoutRaised = false;
        vm.LogoutRequested += () => logoutRaised = true;

        await vm.ReLoginCommand.ExecuteAsync(null);

        vm.IsSessionExpired.Should().BeFalse();
        _auth.Verify(a => a.Logout(), Times.Once);
        logoutRaised.Should().BeTrue();
    }
}
