using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Integration.Tests.DeliveryPhotos;

[Collection(IntegrationCollection.Name)]
public sealed class DeliveryPhotosIntegrationTests : IntegrationTestBase
{
    private const string DriverEmail = "driver@example.com";
    private const string DispatcherEmail = "dispatcher@example.com";
    private const string Password = "Password123!";

    public DeliveryPhotosIntegrationTests(IntegrationTestFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Driver_can_get_presigned_upload_url()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(DriverEmail, Password, UserRole.Driver, warehouseId);
        await AuthenticateAsync(DriverEmail, Password);

        var response = await Client.PostAsync("/api/delivery-photos/upload-url", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UploadUrlDto>();
        body!.PhotoKey.Should().NotBeNullOrEmpty();
        body.UploadUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Dispatcher_cannot_get_upload_url_returns_403()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(DispatcherEmail, Password, UserRole.Dispatcher, warehouseId);
        await AuthenticateAsync(DispatcherEmail, Password);

        var response = await Client.PostAsync("/api/delivery-photos/upload-url", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Unauthenticated_upload_url_request_returns_401()
    {
        var response = await Client.PostAsync("/api/delivery-photos/upload-url", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Driver_can_get_photo_download_url()
    {
        var warehouseId = await SeedWarehouseAsync();
        var driver = await SeedUserAsync(DriverEmail, Password, UserRole.Driver, warehouseId);
        var orderId = await SeedIndividualOrderAsync(warehouseId);
        await SeedDeliveryAttemptAsync(orderId, driver.Id, "abc123.jpg");
        await AuthenticateAsync(DriverEmail, Password);

        var response = await Client.GetAsync("/api/delivery-photos/abc123.jpg");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var url = await response.Content.ReadAsStringAsync();
        url.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Dispatcher_can_get_photo_download_url()
    {
        var warehouseId = await SeedWarehouseAsync();
        await SeedUserAsync(DispatcherEmail, Password, UserRole.Dispatcher, warehouseId);
        var driver = await SeedUserAsync(DriverEmail, Password, UserRole.Driver, warehouseId);
        var orderId = await SeedIndividualOrderAsync(warehouseId);
        await SeedDeliveryAttemptAsync(orderId, driver.Id, "some-photo.jpg");
        await AuthenticateAsync(DispatcherEmail, Password);

        var response = await Client.GetAsync("/api/delivery-photos/some-photo.jpg");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Unauthenticated_download_url_request_returns_401()
    {
        var response = await Client.GetAsync("/api/delivery-photos/some-photo.jpg");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record UploadUrlDto(string PhotoKey, string UploadUrl);
}
