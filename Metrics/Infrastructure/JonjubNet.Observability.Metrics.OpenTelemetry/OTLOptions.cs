using JonjubNet.Observability.Metrics.Shared.Configuration;
using JonjubNet.Observability.Shared.OpenTelemetry;

namespace JonjubNet.Observability.Metrics.OpenTelemetry
{
    /// <summary>
    /// Opciones de configuración para OpenTelemetry Metrics
    /// Hereda de OtlpOptions compartido
    /// </summary>
    public class OTLOptions : MetricsSinkOptions
    {
        // Propiedades heredadas de OtlpOptions (a través de composición)
        // Mantenemos compatibilidad con MetricsSinkOptions
        public new bool Enabled { get; set; } = true;
        public string Endpoint { get; set; } = "http://localhost:4318";
        public OtlpProtocol Protocol { get; set; } = OtlpProtocol.HttpJson;
        public bool EnableCompression { get; set; } = true;
        public int TimeoutSeconds { get; set; } = 30;
    }
}
