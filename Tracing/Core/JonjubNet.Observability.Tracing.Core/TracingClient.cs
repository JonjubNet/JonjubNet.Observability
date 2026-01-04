using JonjubNet.Observability.Tracing.Core.Interfaces;
using JonjubNet.Observability.Shared.Context;
using System.Collections.Concurrent;

namespace JonjubNet.Observability.Tracing.Core
{
    /// <summary>
    /// Implementación del cliente de tracing (Fast Path)
    /// Optimizado: Solo escribe al Registry - todos los sinks leen del Registry
    /// Similar a LoggingClient y MetricsClient
    /// </summary>
    public class TracingClient : ITracingClient
    {
        private readonly TraceRegistry _registry;
        private readonly TraceScopeManager _scopeManager;
        private static readonly AsyncLocal<ISpan?> _currentSpan = new();

        // Cache de operation names internadas para reducir allocations de GC
        private static readonly ConcurrentDictionary<string, string> _internedOperationNames = new();
        private static readonly object _operationNameLock = new();

        public TracingClient(
            TraceRegistry registry,
            TraceScopeManager? scopeManager = null)
        {
            _registry = registry;
            _scopeManager = scopeManager ?? new TraceScopeManager();
        }

        /// <summary>
        /// String interning para operation names (optimización GC)
        /// Similar a InternCategory en LoggingClient
        /// </summary>
        private static string InternOperationName(string operationName)
        {
            if (string.IsNullOrEmpty(operationName))
                return string.Empty;

            // Usar ConcurrentDictionary para thread-safety sin locks en el hot path
            return _internedOperationNames.GetOrAdd(operationName, op => string.Intern(op));
        }

        public ISpan StartSpan(string operationName, SpanKind kind = SpanKind.Internal, Dictionary<string, string>? tags = null)
        {
            // Obtener contexto compartido (optimizado: solo leer si existe)
            var context = ObservabilityContext.Current;
            
            // Mejores prácticas: TraceId para correlación de spans (tracing distribuido)
            // Si hay CorrelationId en el contexto, usarlo como TraceId para mantener correlación
            var traceId = context?.CorrelationId ?? context?.TraceId ?? GenerateTraceId();
            var spanId = GenerateSpanId();
            
            var span = new Span
            {
                SpanId = spanId,
                TraceId = traceId,
                OperationName = InternOperationName(operationName), // Usar string interning
                Kind = kind,
                StartTime = DateTimeOffset.UtcNow,
                Tags = tags ?? new Dictionary<string, string>(),
                CorrelationId = context?.CorrelationId, // Identificador único de la transacción
                SessionId = context?.SessionId
            };

            // Enriquecer tags con CorrelationId del contexto (identificador principal)
            if (context != null)
            {
                // CorrelationId es el identificador principal (siempre debe estar disponible)
                if (!string.IsNullOrEmpty(context.CorrelationId))
                {
                    span.Tags.TryAdd("correlation.id", context.CorrelationId);
                }
                
                // SessionId opcional (no se propaga entre microservicios)
                if (!string.IsNullOrEmpty(context.SessionId))
                {
                    span.Tags.TryAdd("session.id", context.SessionId);
                }
            }

            var spanImpl = new SpanImpl(span, _registry);
            _currentSpan.Value = spanImpl;
            
            // Actualizar contexto con TraceId y SpanId para correlación de spans
            // CorrelationId se mantiene del contexto original
            ObservabilityContext.SetTraceId(traceId);
            ObservabilityContext.SetSpanId(spanId);
            
            return spanImpl;
        }

        public ISpan StartChildSpan(string operationName, SpanKind kind = SpanKind.Internal, Dictionary<string, string>? tags = null)
        {
            var parentSpan = _currentSpan.Value;
            var context = ObservabilityContext.Current;
            
            // Mejores prácticas: TraceId para correlación de spans (tracing distribuido)
            // Usar TraceId del span padre o CorrelationId del contexto
            var traceId = parentSpan?.TraceId ?? context?.CorrelationId ?? context?.TraceId ?? GenerateTraceId();
            var spanId = GenerateSpanId();
            
            // Obtener CorrelationId del contexto o del span padre
            var correlationId = context?.CorrelationId;
            if (string.IsNullOrEmpty(correlationId) && parentSpan != null)
            {
                correlationId = GetCorrelationIdFromSpan(parentSpan);
            }
            
            var span = new Span
            {
                SpanId = spanId,
                TraceId = traceId,
                ParentSpanId = parentSpan?.SpanId,
                OperationName = InternOperationName(operationName), // Usar string interning
                Kind = kind,
                StartTime = DateTimeOffset.UtcNow,
                Tags = tags ?? new Dictionary<string, string>(),
                CorrelationId = correlationId, // Identificador único de la transacción
                SessionId = context?.SessionId
            };

            // Enriquecer tags con CorrelationId (identificador principal)
            if (!string.IsNullOrEmpty(correlationId))
            {
                span.Tags.TryAdd("correlation.id", correlationId);
            }
            
            // SessionId opcional (no se propaga entre microservicios)
            if (!string.IsNullOrEmpty(context?.SessionId))
            {
                span.Tags.TryAdd("session.id", context.SessionId);
            }

            var spanImpl = new SpanImpl(span, _registry);
            _currentSpan.Value = spanImpl;
            
            // Actualizar contexto con nuevo SpanId (mantener TraceId y CorrelationId)
            ObservabilityContext.SetSpanId(spanId);
            
            return spanImpl;
        }

        /// <summary>
        /// Obtiene CorrelationId de un span (helper para evitar duplicación)
        /// </summary>
        private string? GetCorrelationIdFromSpan(ISpan span)
        {
            // El CorrelationId está directamente en el Span, no en Properties
            if (span is SpanImpl spanImpl)
            {
                return spanImpl.GetSpan().CorrelationId;
            }
            return null;
        }

        public ISpan? GetCurrentSpan()
        {
            return _currentSpan.Value;
        }

        public IDisposable BeginScope(string scopeName, Dictionary<string, object?>? properties = null)
        {
            return _scopeManager.BeginScope(scopeName, properties);
        }

        public IDisposable BeginOperation(string operationName, Dictionary<string, string>? tags = null)
        {
            var span = StartSpan(operationName, SpanKind.Internal, tags);
            return new OperationScope(span, operationName);
        }

        private class OperationScope : IDisposable
        {
            private readonly ISpan _span;
            private readonly string _operationName;
            private bool _disposed;

            public OperationScope(ISpan span, string operationName)
            {
                _span = span;
                _operationName = operationName;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _span.Finish();
                _disposed = true;
            }
        }

        /// <summary>
        /// Genera un Trace ID único (128 bits, formato hexadecimal)
        /// Reutiliza TraceIdGenerator para evitar duplicación
        /// </summary>
        private static string GenerateTraceId()
        {
            return TraceIdGenerator.GenerateTraceId();
        }

        /// <summary>
        /// Genera un Span ID único (64 bits, formato hexadecimal)
        /// Reutiliza TraceIdGenerator para evitar duplicación
        /// </summary>
        private static string GenerateSpanId()
        {
            return TraceIdGenerator.GenerateSpanId();
        }
    }
}
