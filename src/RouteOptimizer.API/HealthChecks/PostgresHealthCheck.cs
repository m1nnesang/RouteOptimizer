using Microsoft.Extensions.Diagnostics.HealthChecks;
using RouteOptimizer.Infrastructure.Persistence;

namespace RouteOptimizer.API.HealthChecks;

public sealed class PostgresHealthCheck : IHealthCheck
{
    private readonly AppDbContext _db;

    public PostgresHealthCheck(AppDbContext db) => _db = db;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("PostgreSQL is reachable")
                : HealthCheckResult.Unhealthy("PostgreSQL is not reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL check failed", ex);
        }
    }
}
