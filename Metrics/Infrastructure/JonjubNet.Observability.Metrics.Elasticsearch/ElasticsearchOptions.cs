using JonjubNet.Observability.Metrics.Shared.Configuration;

namespace JonjubNet.Observability.Metrics.Elasticsearch
{
    /// <summary>
    /// Opciones de configuración para el sink de Elasticsearch para Metrics
    /// Similar a ElasticsearchOptions de Logging pero para métricas
    /// </summary>
    public class ElasticsearchOptions : MetricsSinkOptions
    {
        /// <summary>
        /// URL base de Elasticsearch (ej: http://localhost:9200)
        /// </summary>
        public string BaseUrl { get; set; } = "http://localhost:9200";

        /// <summary>
        /// Nombre del índice de Elasticsearch
        /// </summary>
        public string IndexName { get; set; } = "metrics";

        /// <summary>
        /// Tipo de documento (por defecto _doc para ES 7+)
        /// </summary>
        public string DocumentType { get; set; } = "_doc";

        /// <summary>
        /// Tamaño de batch para envío agrupado
        /// </summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// Timeout en segundos
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Usuario para autenticación básica (opcional)
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Contraseña para autenticación básica (opcional)
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Headers adicionales para las peticiones HTTP
        /// </summary>
        public Dictionary<string, string>? Headers { get; set; }
    }
}

