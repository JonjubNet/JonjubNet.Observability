using JonjubNet.Observability.Shared.OpenTelemetry;

namespace JonjubNet.Observability.Logging.OpenTelemetry
{
    /// <summary>
    /// Opciones de configuración para OpenTelemetry Logging
    /// Hereda propiedades comunes de OtlpOptions compartido
    /// </summary>
    public class OTLLogOptions : OtlpOptions
    {
        /// <summary>
        /// Tamaño de batch para envío agrupado
        /// </summary>
        public int BatchSize { get; set; } = 100;
    }
}

