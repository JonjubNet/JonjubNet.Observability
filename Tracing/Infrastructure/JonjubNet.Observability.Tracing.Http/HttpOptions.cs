namespace JonjubNet.Observability.Tracing.Http
{
    /// <summary>
    /// Opciones de configuración para el exporter HTTP para Tracing
    /// Similar a HttpOptions de Logging pero para traces/spans
    /// </summary>
    public class HttpOptions
    {
        /// <summary>
        /// Indica si el exporter está habilitado
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// URL del endpoint HTTP
        /// </summary>
        public string EndpointUrl { get; set; } = "http://localhost:8080/traces";

        /// <summary>
        /// Tamaño de batch para envío agrupado
        /// </summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// Timeout en segundos
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Content-Type por defecto para las peticiones
        /// </summary>
        public string? DefaultContentType { get; set; } = "application/json";

        /// <summary>
        /// Si es true, encripta el payload antes de enviarlo
        /// </summary>
        public bool EncryptPayload { get; set; } = false;

        /// <summary>
        /// Headers adicionales para las peticiones HTTP
        /// </summary>
        public Dictionary<string, string>? Headers { get; set; }
    }
}

