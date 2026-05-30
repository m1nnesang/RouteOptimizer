using FluentAssertions;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Tests;

public class AddressTests
{
    [Fact]
    public void Create_WithValidData_ReturnsSuccess()
    {
        var result = Address.Create("Main St 1", "Amsterdam", "1234AB", "NL");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Street.Should().Be("Main St 1");
        result.Value.City.Should().Be("Amsterdam");
        result.Value.PostalCode.Should().Be("1234AB");
        result.Value.Country.Should().Be("NL");
    }

    [Theory]
    [InlineData("", "Amsterdam", "1234AB", "NL")]
    [InlineData("Main St", "", "1234AB", "NL")]
    [InlineData("Main St", "Amsterdam", "", "NL")]
    [InlineData("Main St", "Amsterdam", "1234AB", "")]
    [InlineData("   ", "Amsterdam", "1234AB", "NL")]
    [InlineData("Main St", "   ", "1234AB", "NL")]
    [InlineData("Main St", "Amsterdam", "   ", "NL")]
    [InlineData("Main St", "Amsterdam", "1234AB", "   ")]
    public void Create_WithEmptyOrWhitespaceField_ReturnsFailure(string street, string city, string postalCode, string country)
    {
        var result = Address.Create(street, city, postalCode, country);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("All fields are required");
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Create_TwoAddressesWithSameData_AreEqual()
    {
        var a1 = Address.Create("Main St", "Amsterdam", "1234AB", "NL").Value!;
        var a2 = Address.Create("Main St", "Amsterdam", "1234AB", "NL").Value!;

        a1.Should().Be(a2);
    }

    [Fact]
    public void Create_TwoAddressesWithDifferentStreet_AreNotEqual()
    {
        var a1 = Address.Create("Main St", "Amsterdam", "1234AB", "NL").Value!;
        var a2 = Address.Create("Other St", "Amsterdam", "1234AB", "NL").Value!;

        a1.Should().NotBe(a2);
    }

    [Fact]
    public void Create_TwoAddressesWithDifferentCity_AreNotEqual()
    {
        var a1 = Address.Create("Main St", "Amsterdam", "1234AB", "NL").Value!;
        var a2 = Address.Create("Main St", "Rotterdam", "1234AB", "NL").Value!;

        a1.Should().NotBe(a2);
    }

    [Fact]
    public void Create_TwoAddressesWithDifferentCountry_AreNotEqual()
    {
        var a1 = Address.Create("Main St", "Amsterdam", "1234AB", "NL").Value!;
        var a2 = Address.Create("Main St", "Amsterdam", "1234AB", "DE").Value!;

        a1.Should().NotBe(a2);
    }
}
