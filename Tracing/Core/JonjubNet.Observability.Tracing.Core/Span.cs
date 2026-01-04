using System.Text.Json.Serialization;

namespace JonjubNet.Observability.Tracing.Core
{
    /// <summary>
    /// Representa un span de tracing (operación individual en un trace)
    /// Similar a StructuredLogEntry pero para tracing distribuido
    /// </summary>
    public class Span
    {
        /// <summary>
        /// ID único del span
        /// </summary>
        public string SpanId { get; set; } = string.Empty;

        /// <summary>
        /// ID del trace (identificador único del trace completo)
        /// </summary>
        public string TraceId { get; set; } = string.Empty;

        /// <summary>
        /// ID del span padre (si existe)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ParentSpanId { get; set; }

        /// <summary>
        /// Nombre de la operación
        /// </summary>
        public string OperationName { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de span (Server, Client, Internal, Producer, Consumer)
        /// </summary>
        public SpanKind Kind { get; set; } = SpanKind.Internal;

        /// <summary>
        /// Estado del span (Unset, Ok, Error)
        /// </summary>
        public SpanStatus Status { get; set; } = SpanStatus.Unset;

        /// <summary>
        /// Mensaje de error (si Status es Error)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Timestamp de inicio
        /// </summary>
        public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Timestamp de fin (si está completado)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? EndTime { get; set; }

        /// <summary>
        /// Duración del span en milisegundos
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? DurationMs { get; set; }

        /// <summary>
        /// Tags del span (key-value pairs para metadata)
        /// </summary>
        public Dictionary<string, string> Tags { get; set; } = new();

        /// <summary>
        /// Eventos del span (logs dentro del span)
        /// </summary>
        public List<SpanEvent> Events { get; set; } = new();

        /// <summary>
        /// Propiedades adicionales (key-value pairs)
        /// </summary>
        public Dictionary<string, object?> Properties { get; set; } = new();

        /// <summary>
        /// Service name (nombre del servicio que generó el span)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ServiceName { get; set; }

        /// <summary>
        /// Resource name (recurso que está siendo trazado)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ResourceName { get; set; }

        /// <summary>
        /// Indica si el span está activo
        /// </summary>
        public bool IsActive { get; set; } = true;

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
    }

    /// <summary>
    /// Tipo de span (según OpenTelemetry)
    /// </summary>
    public enum SpanKind
    {
        /// <summary>
        /// Span interno (operación dentro del servicio)
        /// </summary>
        Internal = 0,

        /// <summary>
        /// Span de servidor (request recibido)
        /// </summary>
        Server = 1,

        /// <summary>
        /// Span de cliente (request enviado)
        /// </summary>
        Client = 2,

        /// <summary>
        /// Span de productor (mensaje enviado)
        /// </summary>
        Producer = 3,

        /// <summary>
        /// Span de consumidor (mensaje recibido)
        /// </summary>
        Consumer = 4
    }

    /// <summary>
    /// Estado del span
    /// </summary>
    public enum SpanStatus
    {
        /// <summary>
        /// Estado no establecido
        /// </summary>
        Unset = 0,

        /// <summary>
        /// Span completado exitosamente
        /// </summary>
        Ok = 1,

        /// <summary>
        /// Span completado con error
        /// </summary>
        Error = 2
    }

    /// <summary>
    /// Evento dentro de un span (log dentro del span)
    /// </summary>
    public class SpanEvent
    {
        /// <summary>
        /// Nombre del evento
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp del evento
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Atributos del evento (key-value pairs)
        /// </summary>
        public Dictionary<string, object?> Attributes { get; set; } = new();
    }
}

