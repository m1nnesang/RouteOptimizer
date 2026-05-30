using FluentAssertions;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Tests;

public class PhoneNumberTests
{
    [Fact]
    public void Create_WithValidNumber_ReturnsSuccess()
    {
        var result = PhoneNumber.Create("+48601234567");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Value.Should().Be("+48601234567");
    }

    [Fact]
    public void Create_WithEmptyString_ReturnsFailure()
    {
        var result = PhoneNumber.Create("");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Phone number cannot be empty");
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Create_WithNull_ReturnsFailure()
    {
        var result = PhoneNumber.Create(null!);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Phone number cannot be empty");
    }

    [Fact]
    public void Create_TwoPhoneNumbersWithSameValue_AreEqual()
    {
        var p1 = PhoneNumber.Create("+48601234567").Value!;
        var p2 = PhoneNumber.Create("+48601234567").Value!;

        p1.Should().Be(p2);
    }

    [Fact]
    public void Create_TwoPhoneNumbersWithDifferentValues_AreNotEqual()
    {
        var p1 = PhoneNumber.Create("+48601234567").Value!;
        var p2 = PhoneNumber.Create("+48698765432").Value!;

        p1.Should().NotBe(p2);
    }
}
