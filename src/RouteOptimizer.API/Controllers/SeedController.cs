using Microsoft.AspNetCore.Mvc;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Entities.Orders;
using DomainUser = RouteOptimizer.Domain.Entities.User;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;
using RouteOptimizer.Infrastructure.Persistence;

namespace RouteOptimizer.API.Controllers;

[ApiController]
[Route("api/seed")]
public class SeedController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IWebHostEnvironment _env;

    public SeedController(AppDbContext db, IPasswordHasher passwordHasher, IWebHostEnvironment env)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _env = env;
    }

    [HttpPost]
    public async Task<IActionResult> Seed(CancellationToken ct)
    {
        if (!_env.IsDevelopment())
            return Forbid();

        if (_db.Users.Any())
            return BadRequest("Database already seeded");

        var address = Address.Create("Połczyńska 121", "Warszawa", "01-304", "Poland").Value!;
        var location = GeoCoordinate.Create(52.2401, 20.9290).Value!;
        var warehouse = Warehouse.Create("Magazyn Warszawa", address, location).Value!;
        await _db.Warehouses.AddAsync(warehouse, ct);

        var password = _passwordHasher.Hash("Password123!");

        var managerPhone = PhoneNumber.Create("+48221112233").Value!;
        var manager = DomainUser.Create("manager@route.local", "Jan", "Kowalski", password, managerPhone,
            UserRole.Manager, null, null).Value!;
        await _db.Users.AddAsync(manager, ct);

        var dispatcherPhone = PhoneNumber.Create("+48224445566").Value!;
        var dispatcher = DomainUser.Create("dispatcher@route.local", "Anna", "Nowak", password, dispatcherPhone,
            UserRole.Dispatcher, warehouse.Id, null).Value!;
        await _db.Users.AddAsync(dispatcher, ct);

        var driverPhone = PhoneNumber.Create("+48227778899").Value!;
        var driverProfile = new DriverProfile(LicenseCategory.C);
        var driver = DomainUser.Create("driver@route.local", "Piotr", "Wiśniewski", password, driverPhone,
            UserRole.Driver, warehouse.Id, driverProfile).Value!;
        await _db.Users.AddAsync(driver, ct);

        var vehicle = Vehicle.Create(warehouse.Id, "Mercedes Sprinter",
            Weight.Create(3500m).Value!, Volume.Create(15m).Value!, LicenseCategory.C,
            new[] { CargoType.General, CargoType.Fragile }).Value!;
        await _db.Vehicles.AddAsync(vehicle, ct);

        var orders = CreateOrders(warehouse.Id);
        await _db.Orders.AddRangeAsync(orders, ct);

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            warehouseId = warehouse.Id,
            managerId = manager.Id,
            dispatcherId = dispatcher.Id,
            driverId = driver.Id,
            vehicleId = vehicle.Id,
            orderIds = orders.Select(o => o.Id),
            password = "Password123!"
        });
    }

    private static List<Order> CreateOrders(Guid warehouseId)
    {
        var morning = DeliveryWindow.Between(new TimeOnly(9, 0), new TimeOnly(12, 0), WindowStrictness.Soft);
        var afternoon = DeliveryWindow.Between(new TimeOnly(13, 0), new TimeOnly(17, 0), WindowStrictness.Soft);

        var businessOne = BusinessOrder.Create(
            warehouseId,
            Address.Create("Plac Defilad 1", "Warszawa", "00-901", "Poland").Value!,
            GeoCoordinate.Create(52.2319, 21.0067).Value!,
            Weight.Create(120m).Value!, Volume.Create(1.5m).Value!, morning,
            PhoneNumber.Create("+48501234567").Value!, CargoType.General,
            "Dostawa do recepcji",
            "Pałac Kultury Sp. z o.o.", "Marek Lewandowski").Value!;

        var businessTwo = BusinessOrder.Create(
            warehouseId,
            Address.Create("Aleja Księcia Józefa Poniatowskiego 1", "Warszawa", "03-901", "Poland").Value!,
            GeoCoordinate.Create(52.2394, 21.0453).Value!,
            Weight.Create(300m).Value!, Volume.Create(4m).Value!, afternoon,
            PhoneNumber.Create("+48502345678").Value!, CargoType.General,
            null,
            "PGE Narodowy", "Katarzyna Zielińska").Value!;

        var businessThree = BusinessOrder.Create(
            warehouseId,
            Address.Create("Wołoska 12", "Warszawa", "02-675", "Poland").Value!,
            GeoCoordinate.Create(52.1810, 21.0040).Value!,
            Weight.Create(80m).Value!, Volume.Create(0.8m).Value!, morning,
            PhoneNumber.Create("+48503456789").Value!, CargoType.Fragile,
            "Towar kruchy - ostrożnie",
            "Galeria Mokotów", "Tomasz Kamiński").Value!;

        var individualOne = IndividualOrder.Create(
            warehouseId,
            Address.Create("Marszałkowska 100", "Warszawa", "00-026", "Poland").Value!,
            GeoCoordinate.Create(52.2280, 21.0120).Value!,
            Weight.Create(15m).Value!, Volume.Create(0.2m).Value!, afternoon,
            PhoneNumber.Create("+48504567890").Value!, CargoType.General,
            null,
            "Agnieszka Wójcik", true).Value!;

        var individualTwo = IndividualOrder.Create(
            warehouseId,
            Address.Create("Złota 59", "Warszawa", "00-120", "Poland").Value!,
            GeoCoordinate.Create(52.2289, 21.0026).Value!,
            Weight.Create(5m).Value!, Volume.Create(0.1m).Value!, morning,
            PhoneNumber.Create("+48505678901").Value!, CargoType.General,
            "Zostawić u sąsiada jeśli nieobecny",
            "Krzysztof Kowalczyk", false).Value!;

        return new List<Order> { businessOne, businessTwo, businessThree, individualOne, individualTwo };
    }
}
