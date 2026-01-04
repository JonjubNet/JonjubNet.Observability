using Microsoft.Extensions.Diagnostics.HealthChecks;
using LoggingHealthCheck = JonjubNet.Observability.Logging.Shared.Health.LoggingHealthCheck;

namespace JonjubNet.Observability.Health
{
    /// <summary>
    /// Health check service para logging
    /// Wrapper para ASP.NET Core Health Checks
    /// </summary>
    public class LoggingHealthCheckService : IHealthCheck
    {
        private readonly LoggingHealthCheck _healthCheck;

        public LoggingHealthCheckService(LoggingHealthCheck healthCheck)
        {
            _healthCheck = healthCheck;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            return _healthCheck.CheckHealthAsync(context, cancellationToken);
        }
    }
}

