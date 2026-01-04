using System.Collections.Concurrent;
using JonjubNet.Observability.Tracing.Core.Interfaces;
using Microsoft.Extensions.Logging;
using JonjubNet.Observability.Tracing.Core.Resilience;

namespace JonjubNet.Observability.Tracing.Core
{
    /// <summary>
    /// Scheduler que exporta traces desde el Registry a los sinks periódicamente
    /// Optimizado: Lee directamente del Registry (sin Bus)
    /// Similar a LogFlushScheduler y MetricFlushScheduler pero para traces
    /// </summary>
    public class TraceFlushScheduler : IDisposable
    {
        private readonly TraceRegistry _registry;
        private readonly IEnumerable<ITraceSink> _sinks;
        private readonly ILogger<TraceFlushScheduler>? _logger;
        private readonly TimeSpan _exportInterval;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly DeadLetterQueue? _deadLetterQueue;
        private readonly RetryPolicy? _retryPolicy;
        private Task? _backgroundTask;
        // Cache de sinks habilitados para evitar ToList() en cada flush
        // Thread-safe: usa volatile para lectura atómica
        private volatile List<ITraceSink>? _cachedEnabledSinks;
        // Thread-safe: usar long (ticks) con Interlocked para lectura/escritura atómica
        private long _lastSinkCacheUpdateTicks = DateTime.MinValue.Ticks;
        private readonly TimeSpan _sinkCacheRefreshInterval = TimeSpan.FromSeconds(30);
        private readonly object _sinkCacheLock = new();

        public TraceFlushScheduler(
            TraceRegistry registry,
            IEnumerable<ITraceSink> sinks,
            TimeSpan? exportInterval = null,
            ILogger<TraceFlushScheduler>? logger = null,
            DeadLetterQueue? deadLetterQueue = null,
            RetryPolicy? retryPolicy = null)
        {
            _registry = registry;
            _sinks = sinks;
            _exportInterval = exportInterval ?? TimeSpan.FromMilliseconds(1000);
            _logger = logger;
            _deadLetterQueue = deadLetterQueue;
            _retryPolicy = retryPolicy;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Inicia el scheduler en background
        /// </summary>
        public void Start()
        {
            if (_backgroundTask != null)
                return;

            _backgroundTask = Task.Run(async () => await ProcessTracesAsync(_cancellationTokenSource.Token));
            _logger?.LogInformation("TraceFlushScheduler started");
        }

        /// <summary>
        /// Procesa traces del Registry periódicamente
        /// </summary>
        private async Task ProcessTracesAsync(CancellationToken cancellationToken)
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
                    _logger?.LogInformation("TraceFlushScheduler stopped");
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error exporting traces from Registry");
                }

                // Esperar antes de la siguiente exportación
                await Task.Delay(_exportInterval, cancellationToken);
            }
        }

        /// <summary>
        /// Exporta traces del Registry a todos los sinks habilitados en paralelo
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
        /// Exporta traces del Registry a un sink específico con retry y DLQ
        /// </summary>
        private async Task ExportToSinkAsync(ITraceSink sink, CancellationToken cancellationToken)
        {
            // Función de exportación que será envuelta por retry
            async Task<bool> ExportOperation()
            {
                await sink.ExportFromRegistryAsync(_registry, cancellationToken);
                return true;
            }

            try
            {
                if (_retryPolicy != null)
                {
                    // Ejecutar con retry policy
                    var result = await _retryPolicy.ExecuteWithResultAsync<bool>(
                        ExportOperation,
                        cancellationToken);

                    if (!result.Success && _deadLetterQueue != null)
                    {
                        // NOTA: No agregar spans a DLQ aquí porque el sink ya procesó el registry
                        // La DLQ se maneja dentro del sink si es necesario
                        // Esto evita duplicados y memory leaks
                        _logger?.LogWarning("Failed to export to sink {SinkName} after {Attempts} retries", 
                            sink.Name, result.TotalAttempts);
                    }
                }
                else
                {
                    // Sin retry - exportación directa
                    await ExportOperation();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exporting to sink {SinkName}", sink.Name);
                
                // NOTA: No agregar spans a DLQ aquí porque el sink ya procesó el registry
                // La DLQ se maneja dentro del sink si es necesario
                // Esto evita duplicados y memory leaks
            }
        }

        /// <summary>
        /// Obtiene sinks habilitados con cache para evitar allocations
        /// Optimizado: evita LINQ allocations usando foreach directo
        /// Thread-safe: usa double-check locking pattern con Interlocked para long
        /// </summary>
        private List<ITraceSink> GetEnabledSinks()
        {
            var nowTicks = DateTime.UtcNow.Ticks;
            // Thread-safe: usar Interlocked.Read para lectura atómica de long
            var lastUpdateTicks = Interlocked.Read(ref _lastSinkCacheUpdateTicks);
            var cached = _cachedEnabledSinks;
            
            // Double-check locking pattern para thread-safety sin overhead innecesario
            if (cached == null || nowTicks - lastUpdateTicks >= _sinkCacheRefreshInterval.Ticks)
            {
                lock (_sinkCacheLock)
                {
                    // Verificar nuevamente dentro del lock (double-check)
                    var currentLastUpdateTicks = Interlocked.Read(ref _lastSinkCacheUpdateTicks);
                    if (_cachedEnabledSinks == null || nowTicks - currentLastUpdateTicks >= _sinkCacheRefreshInterval.Ticks)
                    {
                        // Optimizado: usar foreach directo en lugar de LINQ para evitar enumerable intermedio
                        var enabledSinks = new List<ITraceSink>();
                        foreach (var sink in _sinks)
                        {
                            if (sink.IsEnabled)
                            {
                                enabledSinks.Add(sink);
                            }
                        }
                        _cachedEnabledSinks = enabledSinks;
                        // Thread-safe: usar Interlocked.Exchange para escritura atómica de long
                        Interlocked.Exchange(ref _lastSinkCacheUpdateTicks, nowTicks);
                        return enabledSinks;
                    }
                }
            }
            
            return cached;
        }

        /// <summary>
        /// Obtiene estadísticas de la Dead Letter Queue si está disponible
        /// </summary>
        public DeadLetterQueueStats? GetDeadLetterQueueStats()
        {
            return _deadLetterQueue?.GetStats();
        }

        /// <summary>
        /// Obtiene todos los spans fallidos de la DLQ
        /// </summary>
        public IReadOnlyList<FailedSpan>? GetFailedSpans()
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
