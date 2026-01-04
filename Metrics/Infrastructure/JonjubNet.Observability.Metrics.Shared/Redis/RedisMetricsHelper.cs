using JonjubNet.Observability.Metrics.Core.Interfaces;
using System.Collections.Concurrent;

namespace JonjubNet.Observability.Metrics.Shared.Redis
{
    /// <summary>
    /// Helper optimizado para capturar métricas de Redis.
    /// Thread-safe, sin allocations innecesarias, sin estado persistente, sin memory leaks.
    /// Parte del componente base - puede ser usado por cualquier microservicio.
    /// </summary>
    public static class RedisMetricsHelper
    {
        private static readonly string TenantUnknown = string.Intern("unknown");
        private static readonly string StatusHit = string.Intern("hit");
        private static readonly string StatusMiss = string.Intern("miss");
        private static readonly string StatusError = string.Intern("error");

        // Cache limitado de tenant codes y operations para evitar memory leaks
        private static readonly ConcurrentDictionary<string, string> _tenantCache = new();
        private static readonly ConcurrentDictionary<string, string> _operationCache = new();

        /// <summary>
        /// Registra una operación de Redis con métricas.
        /// </summary>
        public static void RecordCacheOperation(
            IMetricsClient? metrics,
            Func<string?>? getTenantCode,
            string operation,
            string status,
            long durationMs,
            bool success,
            string? metricPrefix = null)
        {
            if (metrics == null) return;

            try
            {
                var tenantCode = getTenantCode?.Invoke() ?? TenantUnknown;
                var internedTenant = GetOrCacheTenant(tenantCode);
                var internedOperation = GetOrCacheOperation(operation);
                var internedStatus = ReferenceEquals(status, StatusHit) ? StatusHit :
                                    ReferenceEquals(status, StatusMiss) ? StatusMiss :
                                    ReferenceEquals(status, StatusError) ? StatusError :
                                    string.Intern(status);

                var prefix = string.IsNullOrEmpty(metricPrefix) ? "redis" : metricPrefix;
                var labels = new Dictionary<string, string>(3)
                {
                    { "operation", internedOperation },
                    { "status", internedStatus },
                    { "tenant", internedTenant }
                };

                if (durationMs > 0)
                {
                    var durationSeconds = durationMs / 1000.0;
                    metrics.RecordHistogram($"{prefix}_operation_duration_seconds", durationSeconds, labels);
                }

                metrics.Increment($"{prefix}_operations_total", 1.0, labels);

                if (!success)
                {
                    metrics.Increment($"{prefix}_operations_errors_total", 1.0, labels);
                }

                if (ReferenceEquals(status, StatusHit))
                {
                    metrics.Increment($"{prefix}_cache_hits_total", 1.0, labels);
                }
                else if (ReferenceEquals(status, StatusMiss))
                {
                    metrics.Increment($"{prefix}_cache_misses_total", 1.0, labels);
                }
            }
            catch
            {
                // Silenciar errores de métricas
            }
        }

        private static string GetOrCacheTenant(string tenantCode)
        {
            if (ReferenceEquals(tenantCode, TenantUnknown))
                return TenantUnknown;

            if (_tenantCache.Count > 1000)
                return tenantCode;

            return _tenantCache.GetOrAdd(tenantCode, key => string.Intern(key));
        }

        private static string GetOrCacheOperation(string operation)
        {
            if (_operationCache.Count > 100)
                return operation;

            return _operationCache.GetOrAdd(operation, key => string.Intern(key));
        }
    }
}

