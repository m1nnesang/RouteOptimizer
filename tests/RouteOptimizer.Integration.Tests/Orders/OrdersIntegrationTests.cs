using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RouteOptimizer.Application.Common;
using RouteOptimizer.Application.Orders.GetOrders;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Integration.Tests.Orders;

[Collection(IntegrationCollection.Name)]
public sealed class OrdersIntegrationTests : IntegrationTestBase
{
    private const string DispatcherEmail = "dispatcher@example.com";
    private const string Password = "Password123!";

    public OrdersIntegrationTests(IntegrationTestFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Create_individual_order_returns_id()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(DispatcherEmail, Password, UserRole.Dispatcher, warehouseId);
        await AuthenticateAsync(DispatcherEmail, Password);

        var response = await Client.PostAsJsonAsync("/api/orders/individual", BuildIndividualOrderRequest(warehouseId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_business_order_returns_id()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(DispatcherEmail, Password, UserRole.Dispatcher, warehouseId);
        await AuthenticateAsync(DispatcherEmail, Password);

        var response = await Client.PostAsJsonAsync("/api/orders/business", new
        {
            WarehouseId = warehouseId,
            CompanyName = "Acme Corp",
            ContactPerson = "Alice",
            Start = "09:00:00",
            End = "17:00:00",
            Street = "2 Business Ave",
            City = "Berlin",
            Postcode = "10115",
            Country = "Germany",
            PhoneNumber = "+12025550111",
            Weight = 5.0m,
            Volume = 2.0m,
            CargoType = nameof(CargoType.General),
            Notes = (string?)null,
            WindowStrictness = "Soft"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Get_orders_returns_paged_list()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(DispatcherEmail, Password, UserRole.Dispatcher, warehouseId);
        await AuthenticateAsync(DispatcherEmail, Password);

        await Client.PostAsJsonAsync("/api/orders/individual", BuildIndividualOrderRequest(warehouseId));
        await Client.PostAsJsonAsync("/api/orders/individual", BuildIndividualOrderRequest(warehouseId));

        var response = await Client.GetAsync("/api/orders?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderListItemDto>>();
        result!.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Get_orders_filters_by_status()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(DispatcherEmail, Password, UserRole.Dispatcher, warehouseId);
        await AuthenticateAsync(DispatcherEmail, Password);

        var orderIdResponse = await Client.PostAsJsonAsync("/api/orders/individual", BuildIndividualOrderRequest(warehouseId));
        var orderId = await orderIdResponse.Content.ReadFromJsonAsync<Guid>();
        await Client.PostAsync($"/api/orders/{orderId}/cancel", null);

        var createdResponse = await Client.GetAsync($"/api/orders?status={nameof(OrderStatus.Created)}");
        var createdResult = await createdResponse.Content.ReadFromJsonAsync<PagedResult<OrderListItemDto>>();
        createdResult!.Items.Should().BeEmpty();

        var cancelledResponse = await Client.GetAsync($"/api/orders?status={nameof(OrderStatus.Cancelled)}");
        var cancelledResult = await cancelledResponse.Content.ReadFromJsonAsync<PagedResult<OrderListItemDto>>();
        cancelledResult!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Get_order_by_id_returns_order_details()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(DispatcherEmail, Password, UserRole.Dispatcher, warehouseId);
        await AuthenticateAsync(DispatcherEmail, Password);

        var createResponse = await Client.PostAsJsonAsync("/api/orders/individual", BuildIndividualOrderRequest(warehouseId));
        var orderId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await Client.GetAsync($"/api/orders/{orderId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await response.Content.ReadFromJsonAsync<OrderDetailDto>();
        order!.Id.Should().Be(orderId);
        order.Status.Should().Be(nameof(OrderStatus.Created));
    }

    [Fact]
    public async Task Get_order_by_id_for_unknown_order_returns_not_found()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(DispatcherEmail, Password, UserRole.Dispatcher, warehouseId);
        await AuthenticateAsync(DispatcherEmail, Password);

        var response = await Client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancel_order_changes_status_to_cancelled()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(DispatcherEmail, Password, UserRole.Dispatcher, warehouseId);
        await AuthenticateAsync(DispatcherEmail, Password);

        var createResponse = await Client.PostAsJsonAsync("/api/orders/individual", BuildIndividualOrderRequest(warehouseId));
        var orderId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var cancelResponse = await Client.PostAsync($"/api/orders/{orderId}/cancel", null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await Client.GetAsync($"/api/orders/{orderId}");
        var order = await getResponse.Content.ReadFromJsonAsync<OrderDetailDto>();
        order!.Status.Should().Be(nameof(OrderStatus.Cancelled));
    }

    [Fact]
    public async Task Unauthenticated_request_returns_unauthorized()
    {
        var response = await Client.GetAsync("/api/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static object BuildIndividualOrderRequest(Guid warehouseId) => new
    {
        WarehouseId = warehouseId,
        Street = "1 Market Street",
        City = "Berlin",
        Postcode = "10115",
        Country = "Germany",
        CustomerName = "John Doe",
        PhoneNumber = "+12025550123",
        Weight = 1.0m,
        Volume = 1.0m,
        CargoType = nameof(CargoType.General),
        AllowLeaveAtDoor = false,
        Notes = (string?)null,
        Start = (TimeOnly?)null,
        End = (TimeOnly?)null,
        WindowStrictness = (string?)null
    };

    private sealed record OrderDetailDto(Guid Id, string Status);
}
