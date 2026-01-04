using JonjubNet.Observability.Tracing.Core.Interfaces;

namespace JonjubNet.Observability.Tracing.Core
{
    /// <summary>
    /// Implementación de ISpan
    /// Thread-safe: usa campos readonly y volatile para disposed
    /// Sin memory leaks: no mantiene referencias persistentes
    /// </summary>
    public class SpanImpl : ISpan
    {
        private readonly Span _span;
        private readonly TraceRegistry _registry;
        private volatile bool _disposed; // Thread-safe: volatile para lectura/escritura atómica

        public SpanImpl(Span span, TraceRegistry registry)
        {
            _span = span;
            _registry = registry;
        }

        /// <summary>
        /// Obtiene el Span interno (para acceso desde TracingClient)
        /// </summary>
        internal Span GetSpan() => _span;

        public string SpanId => _span.SpanId;
        public string? TraceId => _span.TraceId;
        public string? ParentSpanId => _span.ParentSpanId;
        public string OperationName => _span.OperationName;
        public SpanKind Kind => _span.Kind;
        public SpanStatus Status
        {
            get => _span.Status;
            set => _span.Status = value;
        }
        public DateTimeOffset StartTime => _span.StartTime;
        public DateTimeOffset? EndTime => _span.EndTime;
        public long? DurationMs => _span.DurationMs;
        public Dictionary<string, string> Tags => _span.Tags;
        public IList<SpanEvent> Events => _span.Events;
        public bool IsActive => _span.IsActive && !_disposed;

        public ISpan SetTag(string key, string value)
        {
            if (!_disposed)
            {
                _span.Tags[key] = value;
            }
            return this;
        }

        public ISpan SetTags(Dictionary<string, string> tags)
        {
            if (!_disposed && tags != null)
            {
                foreach (var tag in tags)
                {
                    _span.Tags[tag.Key] = tag.Value;
                }
            }
            return this;
        }

        public ISpan AddEvent(string name, Dictionary<string, object?>? attributes = null)
        {
            if (_disposed)
                return this;

            // Optimizado: evitar allocation de Dictionary vacío si no hay atributos
            var spanEvent = new SpanEvent
            {
                Name = name,
                Timestamp = DateTimeOffset.UtcNow,
                Attributes = attributes != null && attributes.Count > 0 
                    ? new Dictionary<string, object?>(attributes) // Copiar para evitar mutaciones externas
                    : new Dictionary<string, object?>()
            };
            
            _span.Events.Add(spanEvent);
            return this;
        }

        public ISpan RecordException(Exception exception)
        {
            if (_disposed || exception == null)
                return this;

            _span.Status = SpanStatus.Error;
            _span.ErrorMessage = exception.Message;
            
            // Optimizado: pre-allocate capacity para atributos de excepción
            var exceptionAttributes = new Dictionary<string, object?>(3)
            {
                ["exception.type"] = exception.GetType().Name,
                ["exception.message"] = exception.Message,
                ["exception.stacktrace"] = exception.StackTrace
            };
            
            _span.Events.Add(new SpanEvent
            {
                Name = "exception",
                Timestamp = DateTimeOffset.UtcNow,
                Attributes = exceptionAttributes
            });
            
            return this;
        }

        public void Finish()
        {
            Finish(DateTimeOffset.UtcNow);
        }

        public void Finish(DateTimeOffset endTime)
        {
            // Thread-safe: verificar disposed con volatile read
            if (_disposed)
                return;

            // Thread-safe: usar Interlocked.CompareExchange para evitar race conditions
            // Solo un thread puede finalizar el span
            if (System.Threading.Interlocked.CompareExchange(ref _disposed, true, false) != false)
                return; // Ya fue finalizado por otro thread

            _span.EndTime = endTime;
            _span.DurationMs = (long)(endTime - _span.StartTime).TotalMilliseconds;
            _span.IsActive = false;

            // Agregar al registry cuando se completa (thread-safe: ConcurrentQueue)
            _registry.AddSpan(_span);
        }

        public void Dispose()
        {
            // Thread-safe: usar Interlocked para evitar double-dispose
            if (System.Threading.Interlocked.CompareExchange(ref _disposed, true, false) != false)
                return; // Ya fue disposed por otro thread

            // Si no se llamó Finish explícitamente, finalizar ahora
            if (_span.IsActive)
            {
                _span.EndTime = DateTimeOffset.UtcNow;
                _span.DurationMs = (long)(_span.EndTime.Value - _span.StartTime).TotalMilliseconds;
                _span.IsActive = false;
                _registry.AddSpan(_span);
            }
        }
    }
}

