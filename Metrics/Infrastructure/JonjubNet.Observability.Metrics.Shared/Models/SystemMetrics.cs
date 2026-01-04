namespace JonjubNet.Observability.Metrics.Shared.Models
{
    /// <summary>
    /// Modelo para métricas del sistema
    /// </summary>
    public class SystemMetrics
    {
        /// <summary>
        /// Tipo de métrica del sistema
        /// </summary>
        public string MetricType { get; set; } = string.Empty;

        /// <summary>
        /// Valor de la métrica
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Unidad de medida
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Instancia o identificador del recurso
        /// </summary>
        public string Instance { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp de la métrica
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Etiquetas adicionales
        /// </summary>
        public Dictionary<string, string> Labels { get; set; } = new();
    }
}

