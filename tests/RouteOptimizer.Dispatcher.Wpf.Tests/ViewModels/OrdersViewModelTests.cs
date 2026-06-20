using System.Net.Http;
using FluentAssertions;
using Moq;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.ViewModels;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf.Tests.ViewModels;

public class OrdersViewModelTests
{
    private readonly Mock<IApiHttpClient> _api = new();
    private readonly Mock<IDialogService> _dialog = new();

    private OrdersViewModel CreateViewModel()
    {
        _api.Setup(a => a.GetAsync<PagedResult<OrderListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<OrderListItem>());
        return new OrdersViewModel(_api.Object, _dialog.Object);
    }

    [Fact]
    public async Task LoadOrders_Success_PopulatesOrders()
    {
        var orders = new List<OrderListItem>
        {
            new() { Id = Guid.NewGuid(), City = "Warszawa" },
            new() { Id = Guid.NewGuid(), City = "Kraków" }
        };
        _api.Setup(a => a.GetAsync<PagedResult<OrderListItem>>("api/orders?pageSize=100", default))
            .ReturnsAsync(new PagedResult<OrderListItem> { Items = orders });
        var vm = new OrdersViewModel(_api.Object, _dialog.Object);

        vm.Orders.Should().HaveCount(2);
        vm.HasError.Should().BeFalse();
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadOrders_NullResult_OrdersEmpty()
    {
        _api.Setup(a => a.GetAsync<PagedResult<OrderListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync((PagedResult<OrderListItem>?)null);
        var vm = new OrdersViewModel(_api.Object, _dialog.Object);

        vm.Orders.Should().BeEmpty();
        vm.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadOrders_HttpError_SetsErrorMessage()
    {
        _api.Setup(a => a.GetAsync<PagedResult<OrderListItem>>(It.IsAny<string>(), default))
            .ThrowsAsync(new HttpRequestException());
        var vm = new OrdersViewModel(_api.Object, _dialog.Object);

        vm.ErrorMessage.Should().Be("Failed to load orders. Please check your connection.");
        vm.HasError.Should().BeTrue();
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadOrders_WithDate_UrlContainsDate()
    {
        string? capturedUrl = null;
        _api.Setup(a => a.GetAsync<PagedResult<OrderListItem>>(It.IsAny<string>(), default))
            .Callback<string, CancellationToken>((url, _) => capturedUrl = url)
            .ReturnsAsync(new PagedResult<OrderListItem>());

        var vm = new OrdersViewModel(_api.Object, _dialog.Object);
        vm.SelectedDate = new DateTime(2025, 3, 15);
        await Task.Yield();

        capturedUrl.Should().Contain("date=2025-03-15");
    }

    [Fact]
    public async Task LoadOrders_WithoutDate_UrlHasNoDate()
    {
        string? capturedUrl = null;
        _api.Setup(a => a.GetAsync<PagedResult<OrderListItem>>(It.IsAny<string>(), default))
            .Callback<string, CancellationToken>((url, _) => capturedUrl = url)
            .ReturnsAsync(new PagedResult<OrderListItem>());

        var vm = new OrdersViewModel(_api.Object, _dialog.Object);
        await Task.Yield();

        capturedUrl.Should().NotContain("date=");
    }

    [Fact]
    public void SelectedDate_Set_HasDateFilterTrue()
    {
        var vm = CreateViewModel();
        vm.SelectedDate = DateTime.Today;
        vm.HasDateFilter.Should().BeTrue();
    }

    [Fact]
    public void ClearDateFilter_ResetsDate()
    {
        var vm = CreateViewModel();
        vm.SelectedDate = DateTime.Today;
        vm.ClearDateFilterCommand.Execute(null);
        vm.SelectedDate.Should().BeNull();
        vm.HasDateFilter.Should().BeFalse();
    }

    [Fact]
    public async Task SelectedOrder_Set_LoadsDeliveryHistory()
    {
        var orderId = Guid.NewGuid();
        _api.Setup(a => a.GetAsync<PagedResult<OrderListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<OrderListItem>());
        _api.Setup(a => a.GetAsync<List<DeliveryAttemptItem>>(It.IsAny<string>(), default))
            .ReturnsAsync([]);
        var vm = new OrdersViewModel(_api.Object, _dialog.Object);

        vm.SelectedOrder = new OrderListItem { Id = orderId };
        await Task.Yield();

        vm.HasSelectedOrder.Should().BeTrue();
        _api.Verify(a => a.GetAsync<List<DeliveryAttemptItem>>($"api/orders/{orderId}/delivery-attempts", default), Times.Once);
    }

    [Fact]
    public async Task CreateOrder_NoWarehouses_SetsErrorAndSkipsPost()
    {
        _api.Setup(a => a.GetAsync<PagedResult<OrderListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<OrderListItem>());
        _api.Setup(a => a.GetAsync<List<WarehouseListItem>>("api/warehouses", default))
            .ReturnsAsync([]);
        var vm = new OrdersViewModel(_api.Object, _dialog.Object);

        await vm.CreateOrderCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Be("Create a warehouse before adding orders.");
        _api.Verify(a => a.PostAsync<CreateBusinessOrderRequest, Guid>(It.IsAny<string>(), It.IsAny<CreateBusinessOrderRequest>(), default), Times.Never);
        _api.Verify(a => a.PostAsync<CreateIndividualOrderRequest, Guid>(It.IsAny<string>(), It.IsAny<CreateIndividualOrderRequest>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateOrder_DialogCancelled_SkipsPost()
    {
        _api.Setup(a => a.GetAsync<PagedResult<OrderListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<OrderListItem>());
        _api.Setup(a => a.GetAsync<List<WarehouseListItem>>("api/warehouses", default))
            .ReturnsAsync([new WarehouseListItem { Id = Guid.NewGuid() }]);
        _dialog.Setup(d => d.ShowCreateOrderDialog(It.IsAny<CreateOrderDialogViewModel>()))
            .Returns(false);
        var vm = new OrdersViewModel(_api.Object, _dialog.Object);

        await vm.CreateOrderCommand.ExecuteAsync(null);

        _api.Verify(a => a.PostAsync<CreateBusinessOrderRequest, Guid>(It.IsAny<string>(), It.IsAny<CreateBusinessOrderRequest>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateOrder_BusinessConfirmed_PostsToBusinessEndpoint()
    {
        var warehouseId = Guid.NewGuid();
        _api.Setup(a => a.GetAsync<PagedResult<OrderListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<OrderListItem>());
        _api.Setup(a => a.GetAsync<List<WarehouseListItem>>("api/warehouses", default))
            .ReturnsAsync([new WarehouseListItem { Id = warehouseId }]);
        _dialog.Setup(d => d.ShowCreateOrderDialog(It.IsAny<CreateOrderDialogViewModel>()))
            .Callback<CreateOrderDialogViewModel>(vm =>
            {
                vm.OrderType = OrderKind.Business;
                vm.Street = "ul. Testowa 1"; vm.City = "Warszawa";
                vm.Postcode = "00-001"; vm.Country = "PL";
                vm.PhoneNumber = "123456789"; vm.WeightText = "1.5";
                vm.VolumeText = "0.5"; vm.CompanyName = "ACME";
                vm.ContactPerson = "Jan"; vm.StartText = "09:00"; vm.EndText = "17:00";
            })
            .Returns(true);
        _api.Setup(a => a.PostAsync<CreateBusinessOrderRequest, Guid>("api/orders/business", It.IsAny<CreateBusinessOrderRequest>(), default))
            .ReturnsAsync(Guid.NewGuid());
        var vm = new OrdersViewModel(_api.Object, _dialog.Object);

        await vm.CreateOrderCommand.ExecuteAsync(null);

        _api.Verify(a => a.PostAsync<CreateBusinessOrderRequest, Guid>("api/orders/business", It.IsAny<CreateBusinessOrderRequest>(), default), Times.Once);
    }

    [Fact]
    public async Task CreateOrder_IndividualConfirmed_PostsToIndividualEndpoint()
    {
        var warehouseId = Guid.NewGuid();
        _api.Setup(a => a.GetAsync<PagedResult<OrderListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<OrderListItem>());
        _api.Setup(a => a.GetAsync<List<WarehouseListItem>>("api/warehouses", default))
            .ReturnsAsync([new WarehouseListItem { Id = warehouseId }]);
        _dialog.Setup(d => d.ShowCreateOrderDialog(It.IsAny<CreateOrderDialogViewModel>()))
            .Callback<CreateOrderDialogViewModel>(vm =>
            {
                vm.OrderType = OrderKind.Individual;
                vm.Street = "ul. Testowa 1"; vm.City = "Warszawa";
                vm.Postcode = "00-001"; vm.Country = "PL";
                vm.PhoneNumber = "123456789"; vm.WeightText = "1.5";
                vm.VolumeText = "0.5"; vm.CustomerName = "Jan Kowalski";
            })
            .Returns(true);
        _api.Setup(a => a.PostAsync<CreateIndividualOrderRequest, Guid>("api/orders/individual", It.IsAny<CreateIndividualOrderRequest>(), default))
            .ReturnsAsync(Guid.NewGuid());
        var vm = new OrdersViewModel(_api.Object, _dialog.Object);

        await vm.CreateOrderCommand.ExecuteAsync(null);

        _api.Verify(a => a.PostAsync<CreateIndividualOrderRequest, Guid>("api/orders/individual", It.IsAny<CreateIndividualOrderRequest>(), default), Times.Once);
    }

    [Fact]
    public async Task CreateOrder_PostThrows_SetsErrorMessage()
    {
        _api.Setup(a => a.GetAsync<PagedResult<OrderListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync(new PagedResult<OrderListItem>());
        _api.Setup(a => a.GetAsync<List<WarehouseListItem>>("api/warehouses", default))
            .ReturnsAsync([new WarehouseListItem { Id = Guid.NewGuid() }]);
        _dialog.Setup(d => d.ShowCreateOrderDialog(It.IsAny<CreateOrderDialogViewModel>()))
            .Callback<CreateOrderDialogViewModel>(vm => { vm.OrderType = OrderKind.Individual; vm.Street = "s"; vm.City = "c"; vm.Postcode = "p"; vm.Country = "c"; vm.PhoneNumber = "1"; vm.WeightText = "1"; vm.VolumeText = "1"; vm.CustomerName = "x"; })
            .Returns(true);
        _api.Setup(a => a.PostAsync<CreateIndividualOrderRequest, Guid>(It.IsAny<string>(), It.IsAny<CreateIndividualOrderRequest>(), default))
            .ThrowsAsync(new HttpRequestException());
        var vm = new OrdersViewModel(_api.Object, _dialog.Object);

        await vm.CreateOrderCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Be("Failed to create order.");
    }
}
