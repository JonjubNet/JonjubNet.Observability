namespace JonjubNet.Observability.Logging.Http
{
    /// <summary>
    /// Opciones de configuración para el sink de HTTP
    /// </summary>
    public class HttpOptions
    {
        /// <summary>
        /// Indica si el sink está habilitado
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// URL del endpoint HTTP
        /// </summary>
        public string EndpointUrl { get; set; } = "http://localhost:8080/logs";

        /// <summary>
        /// Tamaño de batch para envío agrupado
        /// </summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// Timeout en segundos
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Content-Type por defecto
        /// </summary>
        public string? DefaultContentType { get; set; } = "application/json";

        /// <summary>
        /// Headers adicionales para las peticiones HTTP
        /// </summary>
        public Dictionary<string, string>? Headers { get; set; }

        /// <summary>
        /// Indica si se debe encriptar el payload antes de enviarlo
        /// </summary>
        public bool EncryptPayload { get; set; } = false;
    }
}

