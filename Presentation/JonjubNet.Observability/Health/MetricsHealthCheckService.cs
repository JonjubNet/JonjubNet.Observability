using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using JonjubNet.Observability.Metrics.Shared.Health;

namespace JonjubNet.Observability.Health
{
    /// <summary>
    /// Health check service para ASP.NET Core Health Checks
    /// </summary>
    public class MetricsHealthCheckService : IHealthCheck
    {
        private readonly IMetricsHealthCheck _metricsHealthCheck;
        private readonly ILogger<MetricsHealthCheckService>? _logger;

        public MetricsHealthCheckService(
            IMetricsHealthCheck metricsHealthCheck,
            ILogger<MetricsHealthCheckService>? logger = null)
        {
            _metricsHealthCheck = metricsHealthCheck;
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var overallHealth = _metricsHealthCheck.GetOverallHealth();

                if (overallHealth.IsHealthy)
                {
                    var data = new Dictionary<string, object>
                    {
                        ["scheduler_running"] = overallHealth.SchedulerHealth.IsRunning,
                        ["scheduler_healthy"] = overallHealth.SchedulerHealth.IsHealthy,
                        ["sinks_count"] = overallHealth.SinksHealth.Count,
                        ["healthy_sinks"] = overallHealth.SinksHealth.Values.Count(s => s.IsHealthy && s.IsEnabled)
                    };

                    return Task.FromResult(HealthCheckResult.Healthy(
                        overallHealth.OverallStatusMessage ?? "Metrics system is healthy",
                        data));
                }
                else
                {
                    var data = new Dictionary<string, object>
                    {
                        ["scheduler_healthy"] = overallHealth.SchedulerHealth.IsHealthy,
                        ["scheduler_running"] = overallHealth.SchedulerHealth.IsRunning,
                        ["unhealthy_sinks"] = overallHealth.SinksHealth.Values
                            .Where(s => s.IsEnabled && !s.IsHealthy)
                            .Select(s => s.SinkName)
                            .ToArray()
                    };

                    return Task.FromResult(HealthCheckResult.Unhealthy(
                        overallHealth.OverallStatusMessage ?? "Metrics system is unhealthy",
                        data: data));
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Error checking metrics health: {ex.Message}",
                    ex));
            }
        }
    }
}

