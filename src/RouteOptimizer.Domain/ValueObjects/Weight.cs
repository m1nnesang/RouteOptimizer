using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Domain.ValueObjects;

public class Weight : ValueObject
{
    private Weight()
    {
    }

    private Weight(decimal value)
    {
        Value = value;
    }

    public decimal Value { get; }

    public static Result<Weight> Create(decimal value)
    {
        if (value < 0)
            return Result<Weight>.Failure("Weight must be greater than 0");

        return Result<Weight>.Success(new Weight(value));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}