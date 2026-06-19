using System.Net.Http;
using FluentAssertions;
using Moq;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf.Tests.ViewModels.Dialogs;

public class InsertUrgentOrderDialogViewModelTests
{
    private readonly Mock<IApiHttpClient> _api = new();

    [Fact]
    public async Task LoadOrders_PopulatesAvailableOrders()
    {
        var orders = new List<OrderListItem>
        {
            new() { Id = Guid.NewGuid(), City = "Warszawa" }
        };
        _api.Setup(a => a.GetAsync<PagedResult<OrderListItem>>("api/orders?status=Created&pageSize=100", default))
            .ReturnsAsync(new PagedResult<OrderListItem> { Items = orders });
        var vm = new InsertUrgentOrderDialogViewModel(_api.Object);

        await vm.LoadOrdersCommand.ExecuteAsync(null);

        vm.AvailableOrders.Should().HaveCount(1);
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadOrders_HttpError_SetsErrorMessage()
    {
        _api.Setup(a => a.GetAsync<PagedResult<OrderListItem>>(It.IsAny<string>(), default))
            .ThrowsAsync(new HttpRequestException());
        var vm = new InsertUrgentOrderDialogViewModel(_api.Object);

        await vm.LoadOrdersCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Be("Failed to load orders. Please check your connection.");
        vm.HasError.Should().BeTrue();
    }

    [Fact]
    public void HasSelectedOrder_WhenOrderSelected_True()
    {
        var vm = new InsertUrgentOrderDialogViewModel(_api.Object);
        vm.SelectedOrder = new OrderListItem { Id = Guid.NewGuid() };
        vm.HasSelectedOrder.Should().BeTrue();
    }

    [Fact]
    public void HasSelectedOrder_WhenNull_False()
    {
        var vm = new InsertUrgentOrderDialogViewModel(_api.Object);
        vm.HasSelectedOrder.Should().BeFalse();
    }
}
