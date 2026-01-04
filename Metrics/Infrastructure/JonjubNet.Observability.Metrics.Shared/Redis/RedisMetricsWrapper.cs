using JonjubNet.Observability.Metrics.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace JonjubNet.Observability.Metrics.Shared.Redis
{
    /// <summary>
    /// Wrapper genérico para operaciones de Redis con métricas integradas.
    /// Optimizado: sin overhead innecesario, thread-safe, sin memory leaks.
    /// Parte del componente base - puede ser usado por cualquier microservicio.
    /// </summary>
    public class RedisMetricsWrapper
    {
        private readonly IMetricsClient? _metrics;
        private readonly Func<string?>? _getTenantCode;
        private readonly ILogger<RedisMetricsWrapper>? _logger;
        private readonly string? _metricPrefix;

        private static readonly string StatusHit = string.Intern("hit");
        private static readonly string StatusMiss = string.Intern("miss");
        private static readonly string StatusError = string.Intern("error");

        public RedisMetricsWrapper(
            IMetricsClient? metrics = null,
            Func<string?>? getTenantCode = null,
            ILogger<RedisMetricsWrapper>? logger = null,
            string? metricPrefix = null)
        {
            _metrics = metrics;
            _getTenantCode = getTenantCode;
            _logger = logger;
            _metricPrefix = metricPrefix;
        }

        /// <summary>
        /// Ejecuta una operación de cache con métricas.
        /// </summary>
        public async Task<TResult?> ExecuteWithMetricsAsync<TResult>(
            string operation,
            Func<Task<TResult?>> cacheOperation,
            bool isHit = false)
        {
            if (_metrics == null)
            {
                return await cacheOperation();
            }

            var stopwatch = Stopwatch.StartNew();
            var success = true;
            var status = isHit ? StatusHit : StatusMiss;

            try
            {
                var result = await cacheOperation();
                return result;
            }
            catch (Exception ex)
            {
                success = false;
                status = StatusError;
                _logger?.LogWarning(ex, "Error en operación de Redis: {Operation}", operation);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                RedisMetricsHelper.RecordCacheOperation(
                    _metrics,
                    _getTenantCode,
                    operation,
                    status,
                    stopwatch.ElapsedMilliseconds,
                    success,
                    _metricPrefix);
            }
        }

        /// <summary>
        /// Ejecuta una operación de cache sin retorno.
        /// </summary>
        public async Task ExecuteWithMetricsAsync(
            string operation,
            Func<Task> cacheOperation)
        {
            if (_metrics == null)
            {
                await cacheOperation();
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var success = true;
            var status = StatusHit;

            try
            {
                await cacheOperation();
            }
            catch (Exception ex)
            {
                success = false;
                status = StatusError;
                _logger?.LogWarning(ex, "Error en operación de Redis: {Operation}", operation);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                RedisMetricsHelper.RecordCacheOperation(
                    _metrics,
                    _getTenantCode,
                    operation,
                    status,
                    stopwatch.ElapsedMilliseconds,
                    success,
                    _metricPrefix);
            }
        }

        /// <summary>
        /// Registra un cache hit.
        /// </summary>
        public void RecordCacheHit(string operation)
        {
            if (_metrics == null) return;
            RedisMetricsHelper.RecordCacheOperation(
                _metrics,
                _getTenantCode,
                operation,
                StatusHit,
                0,
                true,
                _metricPrefix);
        }

        /// <summary>
        /// Registra un cache miss.
        /// </summary>
        public void RecordCacheMiss(string operation)
        {
            if (_metrics == null) return;
            RedisMetricsHelper.RecordCacheOperation(
                _metrics,
                _getTenantCode,
                operation,
                StatusMiss,
                0,
                true,
                _metricPrefix);
        }
    }
}

