namespace RouteOptimizer.Domain.Common;

public class Result
{
    protected Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    public static Result Success()
    {
        return new Result(true, null);
    }

    public static Result Failure(string error)
    {
        return new Result(false, error);
    }
}

public class Result<T> : Result
{
    private Result(T value) : base(true, null)
    {
        Value = value;
    }

    private Result(string error) : base(false, error)
    {
        Value = default;
    }

    public T? Value { get; }

    public static Result<T> Success(T value)
    {
        return new Result<T>(value);
    }

    public new static Result<T> Failure(string error)
    {
        return new Result<T>(error);
    }
}
