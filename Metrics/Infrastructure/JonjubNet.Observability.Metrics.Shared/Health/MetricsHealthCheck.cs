using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Metrics.Shared.Health
{
    /// <summary>
    /// Implementación del health check para métricas
    /// </summary>
    public class MetricsHealthCheck : IMetricsHealthCheck
    {
        private readonly IEnumerable<IMetricsSink>? _sinks;
        private readonly MetricFlushScheduler? _scheduler;
        private readonly ILogger<MetricsHealthCheck>? _logger;
        private readonly Dictionary<string, SinkHealthTracker> _sinkTrackers = new();
        private SchedulerHealthTracker? _schedulerTracker;

        public MetricsHealthCheck(
            IEnumerable<IMetricsSink>? sinks = null,
            MetricFlushScheduler? scheduler = null,
            Microsoft.Extensions.Options.IOptions<JonjubNet.Observability.Metrics.Shared.Configuration.MetricsOptions>? options = null,
            ILogger<MetricsHealthCheck>? logger = null)
        {
            // ELIMINADO: Bus ya no se necesita - todos los sinks leen del Registry
            _sinks = sinks;
            _scheduler = scheduler;
            _logger = logger;

            // Inicializar trackers para sinks
            if (_sinks != null)
            {
                foreach (var sink in _sinks)
                {
                    _sinkTrackers[sink.Name] = new SinkHealthTracker
                    {
                        SinkName = sink.Name,
                        IsEnabled = sink.IsEnabled
                    };
                }
            }

            // Inicializar tracker para scheduler
            if (_scheduler != null)
            {
                _schedulerTracker = new SchedulerHealthTracker();
            }
        }


        public Dictionary<string, SinkHealthStatus> CheckSinksHealth()
        {
            var result = new Dictionary<string, SinkHealthStatus>();

            if (_sinks == null)
            {
                return result;
            }

            foreach (var sink in _sinks)
            {
                var tracker = _sinkTrackers.GetValueOrDefault(sink.Name);
                if (tracker == null)
                {
                    tracker = new SinkHealthTracker { SinkName = sink.Name };
                    _sinkTrackers[sink.Name] = tracker;
                }

                result[sink.Name] = new SinkHealthStatus
                {
                    SinkName = sink.Name,
                    IsHealthy = tracker.ErrorCount < 10, // Considerar unhealthy si hay más de 10 errores
                    IsEnabled = sink.IsEnabled,
                    Message = tracker.ErrorCount > 0 ? $"Last error: {tracker.LastError}" : null,
                    LastExportTime = tracker.LastExportTime,
                    ExportCount = tracker.ExportCount,
                    ErrorCount = tracker.ErrorCount
                };
            }

            return result;
        }

        public SchedulerHealthStatus CheckSchedulerHealth()
        {
            if (_scheduler == null)
            {
                return new SchedulerHealthStatus
                {
                    IsHealthy = true,
                    IsRunning = false,
                    Message = "Scheduler not configured"
                };
            }

            var tracker = _schedulerTracker;
            if (tracker == null)
            {
                tracker = new SchedulerHealthTracker();
                _schedulerTracker = tracker;
            }

            return new SchedulerHealthStatus
            {
                IsHealthy = tracker.ErrorCount < 5, // Considerar unhealthy si hay más de 5 errores
                IsRunning = true, // Asumimos que está corriendo si existe
                LastFlushTime = tracker.LastFlushTime,
                FlushCount = tracker.FlushCount,
                ErrorCount = tracker.ErrorCount,
                Message = tracker.ErrorCount > 0 ? $"Last error: {tracker.LastError}" : null
            };
        }

        public OverallMetricsHealth GetOverallHealth()
        {
            var schedulerHealth = CheckSchedulerHealth();
            var sinksHealth = CheckSinksHealth();

            var isHealthy = schedulerHealth.IsHealthy &&
                           sinksHealth.Values.All(s => !s.IsEnabled || s.IsHealthy);

            var message = isHealthy
                ? "All metrics components are healthy"
                : "Some metrics components are unhealthy";

            return new OverallMetricsHealth
            {
                IsHealthy = isHealthy,
                SchedulerHealth = schedulerHealth,
                SinksHealth = sinksHealth,
                Message = message,
                OverallStatusMessage = message
            };
        }

        // Métodos internos para actualizar trackers (llamados por el scheduler)
        internal void RecordSinkExport(string sinkName, bool success, Exception? error = null)
        {
            if (!_sinkTrackers.TryGetValue(sinkName, out var tracker))
            {
                tracker = new SinkHealthTracker { SinkName = sinkName };
                _sinkTrackers[sinkName] = tracker;
            }

            tracker.LastExportTime = DateTime.UtcNow;
            if (success)
            {
                tracker.ExportCount++;
            }
            else
            {
                tracker.ErrorCount++;
                tracker.LastError = error?.Message ?? "Unknown error";
            }
        }

        internal void RecordSchedulerFlush(bool success, Exception? error = null)
        {
            if (_schedulerTracker == null)
            {
                _schedulerTracker = new SchedulerHealthTracker();
            }

            _schedulerTracker.LastFlushTime = DateTime.UtcNow;
            if (success)
            {
                _schedulerTracker.FlushCount++;
            }
            else
            {
                _schedulerTracker.ErrorCount++;
                _schedulerTracker.LastError = error?.Message ?? "Unknown error";
            }
        }

        private class SinkHealthTracker
        {
            public string SinkName { get; set; } = string.Empty;
            public bool IsEnabled { get; set; }
            public DateTime LastExportTime { get; set; }
            public int ExportCount { get; set; }
            public int ErrorCount { get; set; }
            public string? LastError { get; set; }
        }

        private class SchedulerHealthTracker
        {
            public DateTime LastFlushTime { get; set; }
            public int FlushCount { get; set; }
            public int ErrorCount { get; set; }
            public string? LastError { get; set; }
        }
    }
}
