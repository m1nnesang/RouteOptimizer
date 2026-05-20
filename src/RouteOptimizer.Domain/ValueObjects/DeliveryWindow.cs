using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Domain.ValueObjects;

public class DeliveryWindow : ValueObject
{
    private DeliveryWindow()
    {
    } // EF Core

    private DeliveryWindow(TimeOnly? start, TimeOnly? end, WindowStrictness strictness, TimeSpan? tolerance)
    {
        Start = start;
        End = end;
        Strictness = strictness;
        Tolerance = tolerance;
    }

    public TimeOnly? Start { get; }
    public TimeOnly? End { get; }
    public WindowStrictness Strictness { get; }
    public TimeSpan? Tolerance { get; }

    private static TimeSpan? ResolveTolerance(WindowStrictness strictness, TimeSpan? tolerance)
    {
        return strictness == WindowStrictness.Strict ? null : tolerance ?? TimeSpan.FromMinutes(10);
    }

    public bool IsViolatedAt(TimeOnly arrivalTime)
    {
        if (End is null) return false;

        if (Strictness == WindowStrictness.Strict)
            return arrivalTime > End;

        var endWithTolerance = End.Value.Add(Tolerance ?? TimeSpan.Zero);

        return arrivalTime > endWithTolerance;
    }

    public static DeliveryWindow Between(TimeOnly start, TimeOnly end, WindowStrictness strictness,
        TimeSpan? tolerance = null)
    {
        var actualTolerance = ResolveTolerance(strictness, tolerance);

        return new DeliveryWindow(start, end, strictness, actualTolerance);
    }

    public static DeliveryWindow From(TimeOnly start, WindowStrictness strictness, TimeSpan? tolerance = null)
    {
        var actualTolerance = ResolveTolerance(strictness, tolerance);

        return new DeliveryWindow(start, null, strictness, actualTolerance);
    }

    public static DeliveryWindow Until(TimeOnly end, WindowStrictness strictness, TimeSpan? tolerance = null)
    {
        var actualTolerance = ResolveTolerance(strictness, tolerance);

        return new DeliveryWindow(null, end, strictness, actualTolerance);
    }

    public static DeliveryWindow AnyTime()
    {
        return new DeliveryWindow(null, null, WindowStrictness.Soft, null);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
        yield return Strictness;
        yield return Tolerance;
    }
}