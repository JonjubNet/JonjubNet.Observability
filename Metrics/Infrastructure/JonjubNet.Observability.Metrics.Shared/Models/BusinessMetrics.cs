namespace JonjubNet.Observability.Metrics.Shared.Models
{
    /// <summary>
    /// Modelo para métricas de negocio
    /// </summary>
    public class BusinessMetrics
    {
        /// <summary>
        /// Nombre de la operación de negocio
        /// </summary>
        public string Operation { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de métrica de negocio
        /// </summary>
        public string MetricType { get; set; } = string.Empty;

        /// <summary>
        /// Valor de la métrica
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Categoría de la métrica
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Indica si la operación fue exitosa
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Duración de la operación en milisegundos
        /// </summary>
        public double DurationMs { get; set; }

        /// <summary>
        /// Etiquetas adicionales
        /// </summary>
        public Dictionary<string, string> Labels { get; set; } = new();
    }
}

