namespace JonjubNet.Observability.Tracing.Core.Interfaces
{
    /// <summary>
    /// Interfaz para un span de tracing
    /// Representa una operación individual en un trace distribuido
    /// </summary>
    public interface ISpan : IDisposable
    {
        /// <summary>
        /// ID único del span
        /// </summary>
        string SpanId { get; }

        /// <summary>
        /// ID del trace padre (si existe)
        /// </summary>
        string? TraceId { get; }

        /// <summary>
        /// ID del span padre (si existe)
        /// </summary>
        string? ParentSpanId { get; }

        /// <summary>
        /// Nombre de la operación
        /// </summary>
        string OperationName { get; }

        /// <summary>
        /// Tipo de span (Server, Client, Internal, Producer, Consumer)
        /// </summary>
        SpanKind Kind { get; }

        /// <summary>
        /// Estado del span (Unset, Ok, Error)
        /// </summary>
        SpanStatus Status { get; set; }

        /// <summary>
        /// Timestamp de inicio
        /// </summary>
        DateTimeOffset StartTime { get; }

        /// <summary>
        /// Timestamp de fin (si está completado)
        /// </summary>
        DateTimeOffset? EndTime { get; }

        /// <summary>
        /// Duración del span en milisegundos
        /// </summary>
        long? DurationMs { get; }

        /// <summary>
        /// Tags del span (key-value pairs)
        /// </summary>
        Dictionary<string, string> Tags { get; }

        /// <summary>
        /// Eventos del span (logs dentro del span)
        /// </summary>
        IList<SpanEvent> Events { get; }

        /// <summary>
        /// Agrega un tag al span
        /// </summary>
        ISpan SetTag(string key, string value);

        /// <summary>
        /// Agrega múltiples tags al span
        /// </summary>
        ISpan SetTags(Dictionary<string, string> tags);

        /// <summary>
        /// Agrega un evento al span
        /// </summary>
        ISpan AddEvent(string name, Dictionary<string, object?>? attributes = null);

        /// <summary>
        /// Agrega un evento de excepción al span
        /// </summary>
        ISpan RecordException(Exception exception);

        /// <summary>
        /// Marca el span como completado
        /// </summary>
        void Finish();

        /// <summary>
        /// Marca el span como completado con un timestamp específico
        /// </summary>
        void Finish(DateTimeOffset endTime);

        /// <summary>
        /// Indica si el span está activo
        /// </summary>
        bool IsActive { get; }
    }
}

