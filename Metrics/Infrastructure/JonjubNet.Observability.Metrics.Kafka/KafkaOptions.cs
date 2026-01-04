using JonjubNet.Observability.Metrics.Shared.Configuration;

namespace JonjubNet.Observability.Metrics.Kafka
{
    /// <summary>
    /// Opciones de configuración para Kafka
    /// </summary>
    public class KafkaOptions : MetricsSinkOptions
    {
        public string Broker { get; set; } = "localhost:9092";
        public string Topic { get; set; } = "metrics";
        public bool EnableCompression { get; set; } = true;
        public int BatchSize { get; set; } = 100;
        public int TimeoutSeconds { get; set; } = 30;
    }
}
