using System.Text.Json.Serialization;

namespace JonjubNet.Observability.Logging.Core
{
    /// <summary>
    /// Entrada de log estructurado
    /// Similar a MetricPoint pero para logs
    /// </summary>
    public class StructuredLogEntry
    {
        /// <summary>
        /// Nivel de log (Trace, Debug, Information, Warning, Error, Critical)
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// Mensaje del log
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Categoría del log
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp del log
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Excepción asociada (si existe)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Exception? Exception { get; set; }

        /// <summary>
        /// Propiedades adicionales (key-value pairs)
        /// </summary>
        public Dictionary<string, object?> Properties { get; set; } = new();

        /// <summary>
        /// Tags para filtrado y agrupación
        /// </summary>
        public Dictionary<string, string> Tags { get; set; } = new();

        /// <summary>
        /// CorrelationId para rastreo distribuido
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CorrelationId { get; set; }

        /// <summary>
        /// RequestId para rastreo de requests
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RequestId { get; set; }

        /// <summary>
        /// SessionId para rastreo de sesiones
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SessionId { get; set; }

        /// <summary>
        /// Usuario que generó el log
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? UserId { get; set; }

        /// <summary>
        /// Tipo de evento (UserAction, SecurityEvent, AuditEvent, etc.)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? EventType { get; set; }

        /// <summary>
        /// Operación asociada (para operaciones con inicio/fin)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Operation { get; set; }

        /// <summary>
        /// Duración de la operación en milisegundos (si aplica)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? DurationMs { get; set; }
    }

    /// <summary>
    /// Niveles de log estándar
    /// </summary>
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Critical = 5
    }
}

