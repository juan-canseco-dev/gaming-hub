using GameHub.Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GameHub.Web.API.HealthChecks;

internal sealed class SqlServerHealthCheck(ApplicationDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("SQL Server is reachable.")
                : HealthCheckResult.Unhealthy("SQL Server is unreachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "SQL Server readiness check failed.",
                exception);
        }
    }
}
