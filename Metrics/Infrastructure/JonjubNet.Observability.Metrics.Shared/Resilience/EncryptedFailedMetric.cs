namespace JonjubNet.Observability.Metrics.Shared.Resilience
{
    /// <summary>
    /// Representa una métrica fallida encriptada para almacenamiento en reposo
    /// </summary>
    public class EncryptedFailedMetric
    {
        /// <summary>
        /// Datos encriptados de la métrica fallida (JSON serializado y encriptado)
        /// </summary>
        public byte[] EncryptedData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Nombre del sink (no encriptado para búsqueda)
        /// </summary>
        public string SinkName { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de fallo (no encriptado para ordenamiento)
        /// </summary>
        public DateTime FailedAt { get; set; }

        /// <summary>
        /// Número de reintentos (no encriptado para estadísticas)
        /// </summary>
        public int RetryCount { get; set; }
    }
}

