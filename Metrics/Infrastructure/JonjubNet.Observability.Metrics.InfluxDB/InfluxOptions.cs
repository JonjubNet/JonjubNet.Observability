using JonjubNet.Observability.Metrics.Shared.Configuration;

namespace JonjubNet.Observability.Metrics.InfluxDB
{
    /// <summary>
    /// Opciones de configuración para InfluxDB
    /// </summary>
    public class InfluxOptions : MetricsSinkOptions
    {
        public string Url { get; set; } = "http://localhost:8086";
        public string? Token { get; set; }
        public string Organization { get; set; } = "default";
        public string Bucket { get; set; } = "metrics";
        public bool EnableCompression { get; set; } = true;
        public int TimeoutSeconds { get; set; } = 30;
    }
}
