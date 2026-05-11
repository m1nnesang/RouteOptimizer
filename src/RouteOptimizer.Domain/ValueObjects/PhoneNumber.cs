using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Domain.ValueObjects;

public class PhoneNumber : ValueObject
{
    public string Value { get; }
    
    private PhoneNumber(string value)
    {
        Value = value;
    }

    public static Result<PhoneNumber> Create(string value)
    {
        if (string.IsNullOrEmpty(value))
            return Result<PhoneNumber>.Failure("Phone number cannot be empty");

        return Result<PhoneNumber>.Success(new PhoneNumber(value));
    }
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}