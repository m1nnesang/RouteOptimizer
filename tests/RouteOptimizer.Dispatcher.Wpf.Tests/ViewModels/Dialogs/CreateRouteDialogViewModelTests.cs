using System.Net.Http;
using FluentAssertions;
using Moq;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf.Tests.ViewModels.Dialogs;

public class CreateRouteDialogViewModelTests
{
    private readonly Mock<IApiHttpClient> _api = new();

    private CreateRouteDialogViewModel CreateViewModel() =>
        new(_api.Object);

    [Fact]
    public void Constructor_DefaultDate_IsToday()
    {
        var vm = CreateViewModel();
        vm.SelectedDate.Date.Should().Be(DateTime.Today);
        vm.RouteDate.Should().Be(DateOnly.FromDateTime(DateTime.Today));
    }

    [Fact]
    public async Task LoadOrders_PopulatesAvailableOrders()
    {
        var orders = new List<OrderListItem>
        {
            new() { Id = Guid.NewGuid(), City = "Warszawa" },
            new() { Id = Guid.NewGuid(), City = "Kraków" }
        };
        _api.Setup(a => a.GetAsync<PagedResult<OrderListItem>>("api/orders?status=Created&pageSize=100", default))
            .ReturnsAsync(new PagedResult<OrderListItem> { Items = orders });
        var vm = CreateViewModel();

        await vm.LoadOrdersCommand.ExecuteAsync(null);

        vm.AvailableOrders.Should().HaveCount(2);
        vm.SelectedCount.Should().Be(0);
        vm.HasSelectedOrders.Should().BeFalse();
    }

    [Fact]
    public async Task LoadOrders_HttpError_SetsErrorMessage()
    {
        _api.Setup(a => a.GetAsync<PagedResult<OrderListItem>>(It.IsAny<string>(), default))
            .ThrowsAsync(new HttpRequestException());
        var vm = CreateViewModel();

        await vm.LoadOrdersCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Be("Failed to load orders. Please check your connection.");
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task SelectingOrder_UpdatesSelectedCount()
    {
        var orders = new List<OrderListItem>
        {
            new() { Id = Guid.NewGuid() },
            new() { Id = Guid.NewGuid() }
        };
        _api.Setup(a => a.GetAsync<PagedResult<OrderListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<OrderListItem> { Items = orders });
        var vm = CreateViewModel();
        await vm.LoadOrdersCommand.ExecuteAsync(null);

        vm.AvailableOrders[0].IsSelected = true;

        vm.SelectedCount.Should().Be(1);
        vm.HasSelectedOrders.Should().BeTrue();
        vm.SelectedOrderIds.Should().ContainSingle().Which.Should().Be(orders[0].Id);
    }

    [Fact]
    public async Task DeselectingOrder_DecreasesCount()
    {
        var orders = new List<OrderListItem> { new() { Id = Guid.NewGuid() } };
        _api.Setup(a => a.GetAsync<PagedResult<OrderListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<OrderListItem> { Items = orders });
        var vm = CreateViewModel();
        await vm.LoadOrdersCommand.ExecuteAsync(null);
        vm.AvailableOrders[0].IsSelected = true;

        vm.AvailableOrders[0].IsSelected = false;

        vm.SelectedCount.Should().Be(0);
        vm.HasSelectedOrders.Should().BeFalse();
    }

    [Fact]
    public async Task ReloadOrders_UnsubscribesOldItems_NoDoubleCount()
    {
        var orders = new List<OrderListItem> { new() { Id = Guid.NewGuid() } };
        _api.Setup(a => a.GetAsync<PagedResult<OrderListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<OrderListItem> { Items = orders });
        var vm = CreateViewModel();
        await vm.LoadOrdersCommand.ExecuteAsync(null);
        var firstItem = vm.AvailableOrders[0];
        await vm.LoadOrdersCommand.ExecuteAsync(null);

        firstItem.IsSelected = true;

        vm.SelectedCount.Should().Be(0);
    }

    [Fact]
    public async Task SelectedOrderIds_ReturnsOnlySelected()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        _api.Setup(a => a.GetAsync<PagedResult<OrderListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<OrderListItem>
            {
                Items = [new() { Id = id1 }, new() { Id = id2 }]
            });
        var vm = CreateViewModel();
        await vm.LoadOrdersCommand.ExecuteAsync(null);
        vm.AvailableOrders[1].IsSelected = true;

        vm.SelectedOrderIds.Should().ContainSingle().Which.Should().Be(id2);
    }
}
