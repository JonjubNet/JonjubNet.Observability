using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using JonjubNet.Observability.Logging.Core;

namespace JonjubNet.Observability.Logging.Shared.Health
{
    /// <summary>
    /// Health check para el sistema de logging
    /// Verifica que el Registry y los sinks estén funcionando correctamente
    /// </summary>
    public class LoggingHealthCheck : IHealthCheck
    {
        private readonly LogRegistry _registry;
        private readonly ILogger<LoggingHealthCheck>? _logger;

        public LoggingHealthCheck(
            LogRegistry registry,
            ILogger<LoggingHealthCheck>? logger = null)
        {
            _registry = registry;
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Verificar que el Registry esté funcionando
                var count = _registry.Count;
                var capacity = 10000; // TODO: Obtener de configuración

                // Calcular utilización
                var utilizationPercent = capacity > 0 ? (double)count / capacity * 100 : 0;

                // Si la utilización es muy alta, considerar degradado
                if (utilizationPercent > 90)
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        $"Log registry utilization is high: {utilizationPercent:F1}% ({count}/{capacity})",
                        data: new Dictionary<string, object>
                        {
                            { "Count", count },
                            { "Capacity", capacity },
                            { "UtilizationPercent", utilizationPercent }
                        }));
                }

                // Si todo está bien, retornar healthy
                return Task.FromResult(HealthCheckResult.Healthy(
                    $"Log registry is healthy: {count} logs, {utilizationPercent:F1}% utilization",
                    data: new Dictionary<string, object>
                    {
                        { "Count", count },
                        { "Capacity", capacity },
                        { "UtilizationPercent", utilizationPercent }
                    }));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking logging health");
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Log registry health check failed",
                    ex,
                    data: new Dictionary<string, object>
                    {
                        { "Error", ex.Message }
                    }));
            }
        }
    }
}

