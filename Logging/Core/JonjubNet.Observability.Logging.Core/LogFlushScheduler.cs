using System.Collections.Concurrent;
using JonjubNet.Observability.Logging.Core.Interfaces;
using Microsoft.Extensions.Logging;
using JonjubNet.Observability.Logging.Core.Resilience;

namespace JonjubNet.Observability.Logging.Core
{
    /// <summary>
    /// Scheduler que exporta logs desde el Registry a los sinks periódicamente
    /// Optimizado: Lee directamente del Registry (sin Bus)
    /// Similar a MetricFlushScheduler pero para logs
    /// </summary>
    public class LogFlushScheduler : IDisposable
    {
        private readonly LogRegistry _registry;
        private readonly IEnumerable<ILogSink> _sinks;
        private readonly ILogger<LogFlushScheduler>? _logger;
        private readonly TimeSpan _exportInterval;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly DeadLetterQueue? _deadLetterQueue;
        private readonly RetryPolicy? _retryPolicy;
        private readonly ISinkCircuitBreakerManager? _circuitBreakerManager;
        private Task? _backgroundTask;
        // Cache de sinks habilitados para evitar ToList() en cada flush
        private List<ILogSink>? _cachedEnabledSinks;
        private DateTime _lastSinkCacheUpdate = DateTime.MinValue;
        private readonly TimeSpan _sinkCacheRefreshInterval = TimeSpan.FromSeconds(30);

        public LogFlushScheduler(
            LogRegistry registry,
            IEnumerable<ILogSink> sinks,
            TimeSpan? exportInterval = null,
            ILogger<LogFlushScheduler>? logger = null,
            DeadLetterQueue? deadLetterQueue = null,
            RetryPolicy? retryPolicy = null,
            ISinkCircuitBreakerManager? circuitBreakerManager = null)
        {
            _registry = registry;
            _sinks = sinks;
            _exportInterval = exportInterval ?? TimeSpan.FromMilliseconds(1000);
            _logger = logger;
            _deadLetterQueue = deadLetterQueue;
            _retryPolicy = retryPolicy;
            _circuitBreakerManager = circuitBreakerManager;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Inicia el scheduler en background
        /// </summary>
        public void Start()
        {
            if (_backgroundTask != null)
                return;

            _backgroundTask = Task.Run(async () => await ProcessLogsAsync(_cancellationTokenSource.Token));
            _logger?.LogInformation("LogFlushScheduler started");
        }

        /// <summary>
        /// Procesa logs del Registry periódicamente
        /// </summary>
        private async Task ProcessLogsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Exportar desde Registry a todos los sinks en paralelo
                    await ExportToAllSinksAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger?.LogInformation("LogFlushScheduler stopped");
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error exporting logs from Registry");
                }

                // Esperar antes de la siguiente exportación
                await Task.Delay(_exportInterval, cancellationToken);
            }
        }

        /// <summary>
        /// Exporta logs del Registry a todos los sinks habilitados en paralelo
        /// </summary>
        private async Task ExportToAllSinksAsync(CancellationToken cancellationToken)
        {
            var enabledSinks = GetEnabledSinks();
            
            if (enabledSinks.Count == 0)
                return;

            // Optimizado: crear array de tasks directamente sin LINQ
            var tasks = new Task[enabledSinks.Count];
            for (int i = 0; i < enabledSinks.Count; i++)
            {
                tasks[i] = ExportToSinkAsync(enabledSinks[i], cancellationToken);
            }
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Exporta logs del Registry a un sink específico con circuit breaker, retry y DLQ
        /// </summary>
        private async Task ExportToSinkAsync(ILogSink sink, CancellationToken cancellationToken)
        {
            // Función de exportación que será envuelta por circuit breaker y retry
            async Task<bool> ExportOperation()
            {
                await sink.ExportFromRegistryAsync(_registry, cancellationToken);
                return true;
            }

            try
            {
                // Si hay circuit breaker manager, usar circuit breaker por sink
                if (_circuitBreakerManager != null)
                {
                    // Ejecutar con circuit breaker del sink
                    if (_retryPolicy != null)
                    {
                        // Combinar circuit breaker con retry policy
                        var result = await _retryPolicy.ExecuteWithResultAsync<bool>(
                            async () => await _circuitBreakerManager.ExecuteWithCircuitBreakerAsync(sink, ExportOperation),
                            cancellationToken);

                        if (!result.Success && _deadLetterQueue != null)
                        {
                            // Obtener logs que fallaron para agregar a DLQ
                            var logs = _registry.GetAllLogs();
                            foreach (var log in logs)
                            {
                                var failedLog = new FailedLog(
                                    log,
                                    sink.Name,
                                    result.TotalAttempts,
                                    result.LastException);
                                _deadLetterQueue.Enqueue(failedLog);
                            }
                            
                            _logger?.LogWarning("Failed to export to sink {SinkName} after {Attempts} retries", 
                                sink.Name, result.TotalAttempts);
                        }
                    }
                    else
                    {
                        // Solo circuit breaker, sin retry
                        await _circuitBreakerManager.ExecuteWithCircuitBreakerAsync(sink, ExportOperation);
                    }
                }
                else if (_retryPolicy != null)
                {
                    // Solo retry policy, sin circuit breaker por sink
                    var result = await _retryPolicy.ExecuteWithResultAsync<bool>(
                        ExportOperation,
                        cancellationToken);

                    if (!result.Success && _deadLetterQueue != null)
                    {
                        // Obtener logs que fallaron para agregar a DLQ
                        var logs = _registry.GetAllLogs();
                        foreach (var log in logs)
                        {
                            var failedLog = new FailedLog(
                                log,
                                sink.Name,
                                result.TotalAttempts,
                                result.LastException);
                            _deadLetterQueue.Enqueue(failedLog);
                        }
                        
                        _logger?.LogWarning("Failed to export to sink {SinkName} after {Attempts} retries", 
                            sink.Name, result.TotalAttempts);
                    }
                }
                else
                {
                    // Sin circuit breaker ni retry - exportación directa
                    await ExportOperation();
                }
            }
            catch (CircuitBreakerOpenException)
            {
                // Circuit breaker está abierto para este sink - no intentar exportar
                _logger?.LogWarning("Circuit breaker is open for sink {SinkName}. Export skipped.", sink.Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exporting to sink {SinkName}", sink.Name);
                
                if (_deadLetterQueue != null)
                {
                    // Obtener logs que fallaron para agregar a DLQ
                    var logs = _registry.GetAllLogs();
                    foreach (var log in logs)
                    {
                        var failedLog = new FailedLog(
                            log,
                            sink.Name,
                            0,
                            ex);
                        _deadLetterQueue.Enqueue(failedLog);
                    }
                }
            }
        }

        /// <summary>
        /// Obtiene sinks habilitados con cache para evitar allocations
        /// Optimizado: evita LINQ allocations usando foreach directo
        /// </summary>
        private List<ILogSink> GetEnabledSinks()
        {
            var now = DateTime.UtcNow;
            
            // Refrescar cache si ha pasado el intervalo o si no existe
            if (_cachedEnabledSinks == null || now - _lastSinkCacheUpdate >= _sinkCacheRefreshInterval)
            {
                // Optimizado: usar foreach directo en lugar de LINQ para evitar enumerable intermedio
                var enabledSinks = new List<ILogSink>();
                foreach (var sink in _sinks)
                {
                    if (sink.IsEnabled)
                    {
                        enabledSinks.Add(sink);
                    }
                }
                _cachedEnabledSinks = enabledSinks;
                _lastSinkCacheUpdate = now;
            }
            
            return _cachedEnabledSinks;
        }

        /// <summary>
        /// Obtiene estadísticas de la Dead Letter Queue si está disponible
        /// </summary>
        public DeadLetterQueueStats? GetDeadLetterQueueStats()
        {
            return _deadLetterQueue?.GetStats();
        }

        /// <summary>
        /// Obtiene todos los logs fallidos de la DLQ
        /// </summary>
        public IReadOnlyList<FailedLog>? GetFailedLogs()
        {
            return _deadLetterQueue?.GetAll();
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _backgroundTask?.Wait(TimeSpan.FromSeconds(5));
            _cancellationTokenSource.Dispose();
            _deadLetterQueue?.Dispose();
        }
    }
}

