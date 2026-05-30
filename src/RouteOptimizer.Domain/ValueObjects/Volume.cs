using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Domain.ValueObjects;

public class Volume : ValueObject
{
    private Volume()
    {
    } // EF Core

    private Volume(decimal value)
    {
        Value = value;
    }

    public decimal Value { get; }

    public static Result<Volume> Create(decimal value)
    {
        if (value < 0)
            return Result<Volume>.Failure("Volume must be greater than 0");

        return Result<Volume>.Success(new Volume(value));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
