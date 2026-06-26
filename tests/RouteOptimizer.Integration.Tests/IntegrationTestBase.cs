using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Auth;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;
using RouteOptimizer.Infrastructure.Persistence;

namespace RouteOptimizer.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;

    protected HttpClient Client { get; }

    protected IntegrationTestBase(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        Client = fixture.CreateClient();
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    protected async Task<User> SeedUserAsync(string email, string password, UserRole role = UserRole.Manager,
        Guid? warehouseId = null)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var phone = PhoneNumber.Create("+12025550123").Value!;
        var profile = role is UserRole.Driver ? new DriverProfile(LicenseCategory.B) : null;
        var user = User.Create(email, "Test", "User", hasher.Hash(password), phone, role, warehouseId, profile).Value!;

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user;
    }

    protected async Task<Guid> SeedVehicleAsync(Guid warehouseId)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var maxWeight = Weight.Create(1000m).Value!;
        var maxVolume = Volume.Create(10m).Value!;
        var vehicle = Vehicle.Create(warehouseId, "Van", maxWeight, maxVolume, LicenseCategory.B,
            new[] { CargoType.General }).Value!;

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        return vehicle.Id;
    }

    protected async Task<Guid> SeedDriverShiftAsync(Guid driverId, Guid vehicleId, Guid warehouseId)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var shiftDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var shift = DriverShift.Create(driverId, vehicleId, warehouseId, shiftDate).Value!;

        db.DriverShifts.Add(shift);
        await db.SaveChangesAsync();

        return shift.Id;
    }

    protected async Task<Guid> SeedIndividualOrderAsync(Guid warehouseId)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var address = Address.Create("2 Test Street", "Berlin", "10115", "Germany").Value!;
        var location = GeoCoordinate.Create(52.5210, 13.4060).Value!;
        var phone = PhoneNumber.Create("+12025550124").Value!;
        var window = DeliveryWindow.Between(new TimeOnly(9, 0), new TimeOnly(12, 0), WindowStrictness.Soft);
        var order = IndividualOrder.Create(warehouseId, address, location,
            Weight.Create(5m).Value!, Volume.Create(0.1m).Value!, window, phone, CargoType.General,
            null, "Test Customer", false).Value!;

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return order.Id;
    }

    protected async Task SeedDeliveryAttemptAsync(Guid orderId, Guid driverId, string photoKey)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var location = GeoCoordinate.Create(52.5200, 13.4050).Value!;
        var attempt = DeliveryAttempt.Create(orderId, driverId, Guid.NewGuid(), location,
            DeliveryOutcome.Delivered, null, null, photoKey).Value!;

        db.DeliveryAttempts.Add(attempt);
        await db.SaveChangesAsync();
    }

    protected async Task<Guid> SeedWarehouseAsync()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var address = Address.Create("1 Test Street", "Berlin", "10115", "Germany").Value!;
        var location = GeoCoordinate.Create(52.5200, 13.4050).Value!;
        var warehouse = Warehouse.Create("Test Warehouse", address, location).Value!;

        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync();

        return warehouse.Id;
    }

    protected async Task AuthenticateAsync(string email, string password)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<AuthTokenDto>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
    }

    protected async Task<AuthTokenDto> LoginAsync(string email, string password)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthTokenDto>())!;
    }

    protected async Task<string> GetInvitationTokenAsync(Guid userId)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var invitation = await db.UserInvitations
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.ExpiresAt)
            .FirstAsync();
        return invitation.Token;
    }

    protected async Task<User?> GetUserByEmailAsync(string email)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Users.FirstOrDefaultAsync(u => u.Email == email);
    }
}
