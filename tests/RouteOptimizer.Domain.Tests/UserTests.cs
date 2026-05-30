using FluentAssertions;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Tests;

public class UserTests
{
    private static PhoneNumber ValidPhone => PhoneNumber.Create("+31612345678").Value!;
    private static DriverProfile ValidDriverProfile => new DriverProfile(LicenseCategory.C);
    private static Guid ValidWarehouseId => Guid.NewGuid();

    // --- Create() happy paths ---

    [Fact]
    public void Create_Driver_WithDriverProfileAndWarehouseId_ReturnsSuccess()
    {
        var result = User.Create(
            "driver@example.com", "Jan", "Kowalski", "hash123",
            ValidPhone, UserRole.Driver, ValidWarehouseId, ValidDriverProfile);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Role.Should().Be(UserRole.Driver);
        result.Value.IsActive.Should().BeTrue();
        result.Value.DriverProfile.Should().NotBeNull();
    }

    [Fact]
    public void Create_Dispatcher_WithWarehouseIdAndNoDriverProfile_ReturnsSuccess()
    {
        var result = User.Create(
            "dispatcher@example.com", "Anna", "Nowak", "hash123",
            ValidPhone, UserRole.Dispatcher, ValidWarehouseId, null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Role.Should().Be(UserRole.Dispatcher);
        result.Value.DriverProfile.Should().BeNull();
    }

    [Fact]
    public void Create_Manager_WithNoWarehouseIdAndNoDriverProfile_ReturnsSuccess()
    {
        var result = User.Create(
            "manager@example.com", "Piotr", "Wisniewski", "hash123",
            ValidPhone, UserRole.Manager, null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Role.Should().Be(UserRole.Manager);
        result.Value.WarehouseId.Should().BeNull();
    }

    // --- Create() failure: Driver rules ---

    [Fact]
    public void Create_Driver_WithoutDriverProfile_ReturnsFailure()
    {
        var result = User.Create(
            "driver@example.com", "Jan", "Kowalski", "hash123",
            ValidPhone, UserRole.Driver, ValidWarehouseId, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("DriverProfile");
    }

    [Fact]
    public void Create_Dispatcher_WithDriverProfile_ReturnsFailure()
    {
        var result = User.Create(
            "dispatcher@example.com", "Anna", "Nowak", "hash123",
            ValidPhone, UserRole.Dispatcher, ValidWarehouseId, ValidDriverProfile);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("DriverProfile");
    }

    [Fact]
    public void Create_Manager_WithDriverProfile_ReturnsFailure()
    {
        var result = User.Create(
            "manager@example.com", "Piotr", "Wisniewski", "hash123",
            ValidPhone, UserRole.Manager, null, ValidDriverProfile);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("DriverProfile");
    }

    // --- Create() failure: Warehouse rules ---

    [Fact]
    public void Create_Driver_WithoutWarehouseId_ReturnsFailure()
    {
        var result = User.Create(
            "driver@example.com", "Jan", "Kowalski", "hash123",
            ValidPhone, UserRole.Driver, null, ValidDriverProfile);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Warehouse");
    }

    [Fact]
    public void Create_Dispatcher_WithoutWarehouseId_ReturnsFailure()
    {
        var result = User.Create(
            "dispatcher@example.com", "Anna", "Nowak", "hash123",
            ValidPhone, UserRole.Dispatcher, null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Warehouse");
    }

    [Fact]
    public void Create_Manager_WithWarehouseId_ReturnsFailure()
    {
        var result = User.Create(
            "manager@example.com", "Piotr", "Wisniewski", "hash123",
            ValidPhone, UserRole.Manager, ValidWarehouseId, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Warehouse");
    }

    // --- Create() failure: empty fields ---

    [Theory]
    [InlineData("", "Jan", "Kowalski")]
    [InlineData("   ", "Jan", "Kowalski")]
    [InlineData("driver@example.com", "", "Kowalski")]
    [InlineData("driver@example.com", "   ", "Kowalski")]
    [InlineData("driver@example.com", "Jan", "")]
    [InlineData("driver@example.com", "Jan", "   ")]
    public void Create_WithEmptyRequiredStringField_ReturnsFailure(string email, string firstName, string lastName)
    {
        var result = User.Create(
            email, firstName, lastName, "hash123",
            ValidPhone, UserRole.Driver, ValidWarehouseId, ValidDriverProfile);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("fields");
    }

    [Fact]
    public void Create_WithEmptyPasswordHash_ReturnsFailure()
    {
        var result = User.Create(
            "driver@example.com", "Jan", "Kowalski", "",
            ValidPhone, UserRole.Driver, ValidWarehouseId, ValidDriverProfile);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Password hash");
    }

    [Fact]
    public void Create_WithWhitespacePasswordHash_ReturnsFailure()
    {
        var result = User.Create(
            "driver@example.com", "Jan", "Kowalski", "   ",
            ValidPhone, UserRole.Driver, ValidWarehouseId, ValidDriverProfile);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Password hash");
    }

    // --- Deactivate() ---

    [Fact]
    public void Deactivate_ActiveUser_SetsIsActiveToFalse()
    {
        var user = User.Create(
            "driver@example.com", "Jan", "Kowalski", "hash123",
            ValidPhone, UserRole.Driver, ValidWarehouseId, ValidDriverProfile).Value!;

        user.Deactivate();

        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_CalledTwice_RemainsInactive()
    {
        var user = User.Create(
            "driver@example.com", "Jan", "Kowalski", "hash123",
            ValidPhone, UserRole.Driver, ValidWarehouseId, ValidDriverProfile).Value!;

        user.Deactivate();
        user.Deactivate();

        user.IsActive.Should().BeFalse();
    }

    // --- SetPassword() ---

    [Fact]
    public void SetPassword_WithValidHash_UpdatesPasswordHash()
    {
        var user = User.Create(
            "driver@example.com", "Jan", "Kowalski", "oldhash",
            ValidPhone, UserRole.Driver, ValidWarehouseId, ValidDriverProfile).Value!;

        user.SetPassword("newhash");

        user.PasswordHash.Should().Be("newhash");
    }

    [Fact]
    public void SetPassword_WithEmptyHash_ThrowsArgumentException()
    {
        var user = User.Create(
            "driver@example.com", "Jan", "Kowalski", "oldhash",
            ValidPhone, UserRole.Driver, ValidWarehouseId, ValidDriverProfile).Value!;

        var act = () => user.SetPassword("");

        act.Should().Throw<ArgumentException>().WithParameterName("passwordHash");
    }

    [Fact]
    public void SetPassword_WithWhitespaceHash_ThrowsArgumentException()
    {
        var user = User.Create(
            "driver@example.com", "Jan", "Kowalski", "oldhash",
            ValidPhone, UserRole.Driver, ValidWarehouseId, ValidDriverProfile).Value!;

        var act = () => user.SetPassword("   ");

        act.Should().Throw<ArgumentException>().WithParameterName("passwordHash");
    }

    // --- IsActive default ---

    [Fact]
    public void Create_NewUser_IsActiveByDefault()
    {
        var result = User.Create(
            "manager@example.com", "Piotr", "Wisniewski", "hash123",
            ValidPhone, UserRole.Manager, null, null);

        result.Value!.IsActive.Should().BeTrue();
    }
}
