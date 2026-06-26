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
        var changed = false;

        var warehouse = db.Warehouses.FirstOrDefault();
        if (warehouse is null)
        {
            var address = Address.Create("Połczyńska 121", "Warszawa", "01-304", "Poland").Value!;
            var location = GeoCoordinate.Create(52.2401, 20.9290).Value!;
            warehouse = Warehouse.Create("Magazyn Warszawa", address, location).Value!;
            await db.Warehouses.AddAsync(warehouse, ct);
            changed = true;
        }

        if (!db.Users.Any())
        {
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

            var driverTwoPhone = PhoneNumber.Create("+48229990011").Value!;
            var driverTwoProfile = new DriverProfile(LicenseCategory.B);
            var driverTwo = DomainUser.Create("driver2@route.local", "Marek", "Zieliński", password, driverTwoPhone,
                UserRole.Driver, warehouse.Id, driverTwoProfile).Value!;
            await db.Users.AddAsync(driverTwo, ct);

            changed = true;
        }

        if (!db.Vehicles.Any())
        {
            await db.Vehicles.AddRangeAsync(CreateVehicles(warehouse.Id), ct);
            changed = true;
        }

        if (!db.Orders.Any())
        {
            await db.Orders.AddRangeAsync(CreateOrders(warehouse.Id), ct);
            changed = true;
        }

        if (changed)
            await db.SaveChangesAsync(ct);
    }

    private static List<Vehicle> CreateVehicles(Guid warehouseId) =>
    [
        Vehicle.Create(warehouseId, "Mercedes Sprinter",
            Weight.Create(3500m).Value!, Volume.Create(15m).Value!, LicenseCategory.C,
            new[] { CargoType.General, CargoType.Fragile }).Value!,
        Vehicle.Create(warehouseId, "Volkswagen Crafter",
            Weight.Create(3000m).Value!, Volume.Create(12m).Value!, LicenseCategory.B,
            new[] { CargoType.General }).Value!,
    ];

    private static List<Order> CreateOrders(Guid warehouseId)
    {
        var morning = DeliveryWindow.Between(new TimeOnly(9, 0), new TimeOnly(12, 0), WindowStrictness.Soft);
        var afternoon = DeliveryWindow.Between(new TimeOnly(13, 0), new TimeOnly(17, 0), WindowStrictness.Soft);

        (string Street, string Postcode, double Lat, double Lon, decimal Weight, decimal Volume,
            bool Morning, CargoType Cargo, string Company, string Contact)[] business =
        {
            ("Plac Defilad 1", "00-901", 52.2319, 21.0067, 120m, 1.5m, true, CargoType.General, "Pałac Kultury Sp. z o.o.", "Marek Lewandowski"),
            ("Aleja Ks. J. Poniatowskiego 1", "03-901", 52.2394, 21.0453, 300m, 4.0m, false, CargoType.General, "PGE Narodowy", "Katarzyna Zielińska"),
            ("Wołoska 12", "02-675", 52.1810, 21.0040, 80m, 0.8m, true, CargoType.Fragile, "Galeria Mokotów", "Tomasz Kamiński"),
            ("Nowy Świat 6", "00-400", 52.2330, 21.0205, 60m, 0.6m, false, CargoType.General, "Empik S.A.", "Anna Mazur"),
            ("Krakowskie Przedmieście 5", "00-068", 52.2405, 21.0150, 45m, 0.5m, true, CargoType.Fragile, "Hotel Bristol", "Paweł Nowicki"),
            ("Grzybowska 60", "00-844", 52.2330, 20.9930, 210m, 2.4m, false, CargoType.General, "Warsaw Spire", "Magdalena Krawczyk"),
            ("Puławska 17", "02-515", 52.2050, 21.0190, 95m, 1.1m, true, CargoType.General, "Plac Unii City", "Robert Sikora"),
            ("Jana Pawła II 22", "00-133", 52.2370, 20.9990, 130m, 1.6m, false, CargoType.Fragile, "Atrium Tower", "Joanna Dąbrowska"),
            ("Świętokrzyska 30", "00-116", 52.2350, 21.0080, 70m, 0.7m, true, CargoType.General, "Q22 Office", "Grzegorz Pawlak"),
            ("Targowa 50", "03-733", 52.2520, 21.0440, 180m, 2.0m, false, CargoType.General, "Centrum Praskie Koneser", "Ewa Wróbel"),
            ("Domaniewska 39", "02-672", 52.1830, 21.0010, 110m, 1.3m, true, CargoType.Fragile, "Mokotów Nova", "Łukasz Adamski"),
            ("Czerniakowska 100", "00-454", 52.2200, 21.0470, 240m, 2.8m, false, CargoType.General, "Royal Wilanów", "Natalia Górska"),
        };

        (string Street, string Postcode, double Lat, double Lon, decimal Weight, decimal Volume,
            bool Morning, CargoType Cargo, string Customer, bool LeaveAtDoor)[] individual =
        {
            ("Marszałkowska 100", "00-026", 52.2280, 21.0120, 15m, 0.2m, false, CargoType.General, "Agnieszka Wójcik", true),
            ("Złota 59", "00-120", 52.2289, 21.0026, 5m, 0.1m, true, CargoType.General, "Krzysztof Kowalczyk", false),
            ("Mokotowska 15", "00-640", 52.2230, 21.0150, 8m, 0.1m, false, CargoType.Fragile, "Barbara Lewandowska", false),
            ("Solec 22", "00-410", 52.2310, 21.0380, 12m, 0.2m, true, CargoType.General, "Michał Jankowski", true),
            ("Wilcza 30", "00-544", 52.2230, 21.0120, 20m, 0.3m, false, CargoType.General, "Karolina Szymańska", true),
            ("Hoża 50", "00-682", 52.2250, 21.0100, 6m, 0.1m, true, CargoType.Fragile, "Piotr Mazurek", false),
            ("Emilii Plater 53", "00-113", 52.2310, 21.0030, 18m, 0.25m, false, CargoType.General, "Aleksandra Kowal", true),
            ("Chmielna 25", "00-021", 52.2300, 21.0090, 9m, 0.15m, true, CargoType.General, "Damian Wieczorek", false),
        };

        var orders = new List<Order>();
        var phone = 510000000;

        foreach (var b in business)
            orders.Add(BusinessOrder.Create(
                warehouseId,
                Address.Create(b.Street, "Warszawa", b.Postcode, "Poland").Value!,
                GeoCoordinate.Create(b.Lat, b.Lon).Value!,
                Weight.Create(b.Weight).Value!, Volume.Create(b.Volume).Value!,
                b.Morning ? morning : afternoon,
                PhoneNumber.Create($"+48{phone++}").Value!, b.Cargo,
                null, b.Company, b.Contact).Value!);

        foreach (var i in individual)
            orders.Add(IndividualOrder.Create(
                warehouseId,
                Address.Create(i.Street, "Warszawa", i.Postcode, "Poland").Value!,
                GeoCoordinate.Create(i.Lat, i.Lon).Value!,
                Weight.Create(i.Weight).Value!, Volume.Create(i.Volume).Value!,
                i.Morning ? morning : afternoon,
                PhoneNumber.Create($"+48{phone++}").Value!, i.Cargo,
                null, i.Customer, i.LeaveAtDoor).Value!);

        return orders;
    }
}
