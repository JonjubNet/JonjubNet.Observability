namespace JonjubNet.Observability.Shared.OpenTelemetry
{
    /// <summary>
    /// Opciones base compartidas para OpenTelemetry
    /// Contiene propiedades comunes entre Metrics y Logging
    /// </summary>
    public abstract class OtlpOptions
    {
        /// <summary>
        /// Habilitar el sink/exporter
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Endpoint del OpenTelemetry Collector
        /// </summary>
        public string Endpoint { get; set; } = "http://localhost:4318";

        /// <summary>
        /// Protocolo OTLP a usar
        /// </summary>
        public OtlpProtocol Protocol { get; set; } = OtlpProtocol.HttpJson;

        /// <summary>
        /// Habilitar compresión GZip
        /// </summary>
        public bool EnableCompression { get; set; } = true;

        /// <summary>
        /// Timeout en segundos
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;
    }
}

