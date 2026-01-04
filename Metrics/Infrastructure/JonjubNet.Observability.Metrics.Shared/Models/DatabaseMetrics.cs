namespace JonjubNet.Observability.Metrics.Shared.Models
{
    /// <summary>
    /// Modelo para métricas de base de datos
    /// </summary>
    public class DatabaseMetrics
    {
        /// <summary>
        /// Tipo de operación (SELECT, INSERT, UPDATE, DELETE, etc.)
        /// </summary>
        public string Operation { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de la tabla
        /// </summary>
        public string Table { get; set; } = string.Empty;

        /// <summary>
        /// Duración de la consulta en milisegundos
        /// </summary>
        public double DurationMs { get; set; }

        /// <summary>
        /// Número de registros afectados
        /// </summary>
        public int RecordsAffected { get; set; }

        /// <summary>
        /// Indica si la operación fue exitosa
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Nombre de la base de datos
        /// </summary>
        public string Database { get; set; } = string.Empty;

        /// <summary>
        /// Etiquetas adicionales
        /// </summary>
        public Dictionary<string, string> Labels { get; set; } = new();
    }
}

