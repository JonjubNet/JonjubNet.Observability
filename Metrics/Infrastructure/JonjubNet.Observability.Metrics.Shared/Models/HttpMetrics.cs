namespace JonjubNet.Observability.Metrics.Shared.Models
{
    /// <summary>
    /// Modelo para métricas HTTP
    /// </summary>
    public class HttpMetrics
    {
        /// <summary>
        /// Método HTTP
        /// </summary>
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// Endpoint o ruta
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Código de estado HTTP
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Duración de la respuesta en milisegundos
        /// </summary>
        public double DurationMs { get; set; }

        /// <summary>
        /// Tamaño de la solicitud en bytes
        /// </summary>
        public long RequestSizeBytes { get; set; }

        /// <summary>
        /// Tamaño de la respuesta en bytes
        /// </summary>
        public long ResponseSizeBytes { get; set; }

        /// <summary>
        /// Etiquetas adicionales
        /// </summary>
        public Dictionary<string, string> Labels { get; set; } = new();
    }
}

