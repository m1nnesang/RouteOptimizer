using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;
using RouteOptimizer.Infrastructure.Persistence;
using DomainUser = RouteOptimizer.Domain.Entities.User;

namespace RouteOptimizer.API;

public static class DevDataSeeder
{
    public const string DefaultPassword = "Password123!";

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher passwordHasher, CancellationToken ct = default)
    {
        if (db.Users.Any())
            return;

        var address = Address.Create("Połczyńska 121", "Warszawa", "01-304", "Poland").Value!;
        var location = GeoCoordinate.Create(52.2401, 20.9290).Value!;
        var warehouse = Warehouse.Create("Magazyn Warszawa", address, location).Value!;
        await db.Warehouses.AddAsync(warehouse, ct);

        var password = passwordHasher.Hash(DefaultPassword);

        var managerPhone = PhoneNumber.Create("+48221112233").Value!;
        var manager = DomainUser.Create("manager@route.local", "Jan", "Kowalski", password, managerPhone,
            UserRole.Manager, null, null).Value!;
        await db.Users.AddAsync(manager, ct);

        var dispatcherPhone = PhoneNumber.Create("+48224445566").Value!;
        var dispatcher = DomainUser.Create("dispatcher@route.local", "Anna", "Nowak", password, dispatcherPhone,
            UserRole.Dispatcher, warehouse.Id, null).Value!;
        await db.Users.AddAsync(dispatcher, ct);

        var driverPhone = PhoneNumber.Create("+48227778899").Value!;
        var driverProfile = new DriverProfile(LicenseCategory.C);
        var driver = DomainUser.Create("driver@route.local", "Piotr", "Wiśniewski", password, driverPhone,
            UserRole.Driver, warehouse.Id, driverProfile).Value!;
        await db.Users.AddAsync(driver, ct);

        var vehicle = Vehicle.Create(warehouse.Id, "Mercedes Sprinter",
            Weight.Create(3500m).Value!, Volume.Create(15m).Value!, LicenseCategory.C,
            new[] { CargoType.General, CargoType.Fragile }).Value!;
        await db.Vehicles.AddAsync(vehicle, ct);

        var orders = CreateOrders(warehouse.Id);
        await db.Orders.AddRangeAsync(orders, ct);

        await db.SaveChangesAsync(ct);
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
