using Microsoft.AspNetCore.Mvc;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Entities;
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

        var address = Address.Create("Main Street 1", "Amsterdam", "1000AA", "Netherlands").Value!;
        var location = GeoCoordinate.Create(52.3676, 4.9041).Value!;
        var warehouse = Warehouse.Create("Main Warehouse", address, location).Value!;
        await _db.Warehouses.AddAsync(warehouse, ct);

        var phone = PhoneNumber.Create("+31612345678").Value!;
        var password = _passwordHasher.Hash("Password123!");

        var manager = DomainUser.Create("manager@route.local", "Ivan", "Manager", password, phone,
            UserRole.Manager, null, null).Value!;
        await _db.Users.AddAsync(manager, ct);

        var dispatcher = DomainUser.Create("dispatcher@route.local", "Anna", "Dispatcher", password, phone,
            UserRole.Dispatcher, warehouse.Id, null).Value!;
        await _db.Users.AddAsync(dispatcher, ct);

        var driverProfile = new DriverProfile(LicenseCategory.B);
        var driver = DomainUser.Create("driver@route.local", "Peter", "Driver", password, phone,
            UserRole.Driver, warehouse.Id, driverProfile).Value!;
        await _db.Users.AddAsync(driver, ct);

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            warehouseId = warehouse.Id,
            managerId = manager.Id,
            dispatcherId = dispatcher.Id,
            driverId = driver.Id,
            password = "Password123!"
        });
    }
}
