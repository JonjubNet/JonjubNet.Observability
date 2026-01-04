using System.Threading;

namespace JonjubNet.Observability.Shared.Context
{
    /// <summary>
    /// Contexto compartido para correlación entre Logging, Metrics y Tracing
    /// Usa AsyncLocal para mantener contexto por thread/async flow
    /// Thread-safe, sin locks, optimizado para performance
    /// </summary>
    public static class ObservabilityContext
    {
        private static readonly AsyncLocal<ObservabilityContextData?> _current = new();

        /// <summary>
        /// Obtiene el contexto actual (thread-safe, sin locks)
        /// </summary>
        public static ObservabilityContextData? Current => _current.Value;

        /// <summary>
        /// Establece el contexto actual
        /// </summary>
        public static void Set(ObservabilityContextData context)
        {
            _current.Value = context;
        }

        /// <summary>
        /// Establece CorrelationId (optimizado: reutiliza contexto existente)
        /// </summary>
        public static void SetCorrelationId(string correlationId)
        {
            var current = _current.Value ?? new ObservabilityContextData();
            current.CorrelationId = correlationId;
            _current.Value = current;
        }

        /// <summary>
        /// Establece RequestId (optimizado: reutiliza contexto existente)
        /// </summary>
        public static void SetRequestId(string requestId)
        {
            var current = _current.Value ?? new ObservabilityContextData();
            current.RequestId = requestId;
            _current.Value = current;
        }

        /// <summary>
        /// Establece TraceId (optimizado: reutiliza contexto existente)
        /// </summary>
        public static void SetTraceId(string traceId)
        {
            var current = _current.Value ?? new ObservabilityContextData();
            current.TraceId = traceId;
            _current.Value = current;
        }

        /// <summary>
        /// Establece SpanId (optimizado: reutiliza contexto existente)
        /// </summary>
        public static void SetSpanId(string spanId)
        {
            var current = _current.Value ?? new ObservabilityContextData();
            current.SpanId = spanId;
            _current.Value = current;
        }

        /// <summary>
        /// Establece SessionId (optimizado: reutiliza contexto existente)
        /// </summary>
        public static void SetSessionId(string sessionId)
        {
            var current = _current.Value ?? new ObservabilityContextData();
            current.SessionId = sessionId;
            _current.Value = current;
        }

        /// <summary>
        /// Establece UserId (optimizado: reutiliza contexto existente)
        /// </summary>
        public static void SetUserId(string userId)
        {
            var current = _current.Value ?? new ObservabilityContextData();
            current.UserId = userId;
            _current.Value = current;
        }

        /// <summary>
        /// Limpia el contexto actual (optimizado: solo asigna null)
        /// </summary>
        public static void Clear()
        {
            _current.Value = null;
        }
    }

    /// <summary>
    /// Datos del contexto de observabilidad
    /// Inmutable después de creación (readonly fields) para thread-safety
    /// </summary>
    public class ObservabilityContextData
    {
        public string? CorrelationId { get; set; }
        public string? RequestId { get; set; }
        public string? SessionId { get; set; }
        public string? TraceId { get; set; }
        public string? SpanId { get; set; }
        public string? UserId { get; set; }
    }
}

