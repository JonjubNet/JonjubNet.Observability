using JonjubNet.Observability.Shared.OpenTelemetry;

namespace JonjubNet.Observability.Tracing.OpenTelemetry
{
    /// <summary>
    /// Opciones de configuración para OpenTelemetry Tracing
    /// Hereda propiedades comunes de OtlpOptions compartido
    /// </summary>
    public class OTLTraceOptions : OtlpOptions
    {
        /// <summary>
        /// Tamaño de batch para envío agrupado
        /// </summary>
        public int BatchSize { get; set; } = 100;
    }
}

