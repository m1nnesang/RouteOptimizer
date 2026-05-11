namespace RouteOptimizer.Domain.Common;

public abstract class ValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();
    
    public override bool Equals(object? obj)
    {
       if (obj is null || GetType() != obj.GetType()) return false;
       
       return ((ValueObject)obj)
           .GetEqualityComponents()
           .SequenceEqual(GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(0, (hash, obj) =>
                HashCode.Combine(hash, obj?.GetHashCode() ?? 0));
    }

    public static bool operator == (ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        
        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
    
}