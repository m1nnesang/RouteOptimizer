using FluentAssertions;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Tests;

public class AddressTests
{
    [Fact]
    public void Create_WithValidData_ReturnsSuccess()
    {
        var result = Address.Create("ul. Marszałkowska 1", "Warszawa", "00-001", "Poland");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Street.Should().Be("ul. Marszałkowska 1");
        result.Value.City.Should().Be("Warszawa");
        result.Value.PostalCode.Should().Be("00-001");
        result.Value.Country.Should().Be("Poland");
    }

    [Theory]
    [InlineData("", "Warszawa", "00-001", "Poland")]
    [InlineData("ul. Marszałkowska", "", "00-001", "Poland")]
    [InlineData("ul. Marszałkowska", "Warszawa", "", "Poland")]
    [InlineData("ul. Marszałkowska", "Warszawa", "00-001", "")]
    [InlineData("   ", "Warszawa", "00-001", "Poland")]
    [InlineData("ul. Marszałkowska", "   ", "00-001", "Poland")]
    [InlineData("ul. Marszałkowska", "Warszawa", "   ", "Poland")]
    [InlineData("ul. Marszałkowska", "Warszawa", "00-001", "   ")]
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
        var a1 = Address.Create("ul. Marszałkowska", "Warszawa", "00-001", "Poland").Value!;
        var a2 = Address.Create("ul. Marszałkowska", "Warszawa", "00-001", "Poland").Value!;

        a1.Should().Be(a2);
    }

    [Fact]
    public void Create_TwoAddressesWithDifferentStreet_AreNotEqual()
    {
        var a1 = Address.Create("ul. Marszałkowska", "Warszawa", "00-001", "Poland").Value!;
        var a2 = Address.Create("ul. Krakowska", "Warszawa", "00-001", "Poland").Value!;

        a1.Should().NotBe(a2);
    }

    [Fact]
    public void Create_TwoAddressesWithDifferentCity_AreNotEqual()
    {
        var a1 = Address.Create("ul. Marszałkowska", "Warszawa", "00-001", "Poland").Value!;
        var a2 = Address.Create("ul. Marszałkowska", "Kraków", "00-001", "Poland").Value!;

        a1.Should().NotBe(a2);
    }

    [Fact]
    public void Create_TwoAddressesWithDifferentCountry_AreNotEqual()
    {
        var a1 = Address.Create("ul. Marszałkowska", "Warszawa", "00-001", "Poland").Value!;
        var a2 = Address.Create("ul. Marszałkowska", "Warszawa", "00-001", "Germany").Value!;

        a1.Should().NotBe(a2);
    }
}
