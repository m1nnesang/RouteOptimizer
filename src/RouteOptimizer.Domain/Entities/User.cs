using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Domain.Entities;

public class User : AggregateRoot<Guid>
{
    public string Email { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string PasswordHash { get; }
    
    public PhoneNumber PhoneNumber { get; }
    public UserRole Role { get; }
    public Guid? WarehouseId { get; }
    
    public DriverProfile? DriverProfile { get; }

    public bool IsActive { get; private set; }

    private User(Guid id ,string email, string firstName, string lastName, string passwordHash, PhoneNumber phoneNumber,
        UserRole role, Guid? warehouseId, DriverProfile? driverProfile) : base(id)
    {
        if (role == UserRole.Driver && driverProfile is null)
            throw new InvalidOperationException($"{nameof(DriverProfile)} is required for Driver role");

        if (role != UserRole.Driver && driverProfile is not null)
            throw new ArgumentException($"Only Driver can have a {nameof(DriverProfile)}");
        
        if (role != UserRole.Manager && warehouseId is  null)
            throw new InvalidOperationException($"Warehouse is required for Driver and Dispatcher ");
         

        (Email, FirstName, LastName, PasswordHash, PhoneNumber, Role, WarehouseId, DriverProfile) 
            = (email, firstName, lastName, passwordHash, phoneNumber, role, warehouseId, driverProfile);
        
        IsActive = true;
    }

    public static Result<User> Create(string email, string firstName, string lastName, string passwordHash,
        PhoneNumber phoneNumber, UserRole role, Guid? warehouseId, DriverProfile? driverProfile)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName))
            
            return Result<User>.Failure("All fields need to be filled");
        
        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result<User>.Failure("Password hash cannot be empty");

        return Result<User>.Success(new User(Guid.NewGuid(), email, firstName, lastName, passwordHash, phoneNumber,
            role, warehouseId, driverProfile));
    }

    public void Deactivate()
    {
        IsActive = false;
    }

}