namespace JonjubNet.Observability.Metrics.Shared.Health
{
    /// <summary>
    /// Health check para el sistema de métricas
    /// </summary>
    public interface IMetricsHealthCheck
    {
        /// <summary>
        /// Verifica el estado de todos los sinks
        /// </summary>
        Dictionary<string, SinkHealthStatus> CheckSinksHealth();

        /// <summary>
        /// Verifica el estado del scheduler
        /// </summary>
        SchedulerHealthStatus CheckSchedulerHealth();

        /// <summary>
        /// Obtiene el estado general del sistema de métricas
        /// </summary>
        OverallMetricsHealth GetOverallHealth();
    }

    /// <summary>
    /// Estado de salud del sistema de métricas (Registry-based)
    /// </summary>
    public class MetricsHealthStatus
    {
        public bool IsHealthy { get; set; }
        public bool IsSaturated { get; set; }
        public int QueueCapacity { get; set; }
        public int CurrentQueueSize { get; set; }
        public double QueueUtilizationPercent { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// Estado de salud de un sink
    /// </summary>
    public class SinkHealthStatus
    {
        public string SinkName { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public bool IsEnabled { get; set; }
        public string? Message { get; set; }
        public DateTime LastExportTime { get; set; }
        public int ExportCount { get; set; }
        public int ErrorCount { get; set; }
    }

    /// <summary>
    /// Estado de salud del scheduler
    /// </summary>
    public class SchedulerHealthStatus
    {
        public bool IsHealthy { get; set; }
        public bool IsRunning { get; set; }
        public DateTime LastFlushTime { get; set; }
        public int FlushCount { get; set; }
        public int ErrorCount { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// Estado general de salud del sistema de métricas
    /// </summary>
    public class OverallMetricsHealth
    {
        public bool IsHealthy { get; set; }
        public SchedulerHealthStatus SchedulerHealth { get; set; } = new();
        public Dictionary<string, SinkHealthStatus> SinksHealth { get; set; } = new();
        public string? Message { get; set; }
        public string? OverallStatusMessage { get; set; }
    }
}
