namespace RouteOptimizer.Domain.Entities;

public sealed class IdempotencyRecord
{
    public Guid Id { get; private set; }
    public string Key { get; private set; }
    public int StatusCode { get; private set; }
    public string Response { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private IdempotencyRecord(Guid id, string key, int statusCode, string response, DateTime createdAt) => (Id, Key, StatusCode, Response, CreatedAt) = (id, key, statusCode, response, createdAt);

    public static IdempotencyRecord Create(string key, int statusCode, string response)
    {
        var now = DateTime.UtcNow;
        return new IdempotencyRecord(Guid.NewGuid(), key, statusCode, response, now);
    }
}
