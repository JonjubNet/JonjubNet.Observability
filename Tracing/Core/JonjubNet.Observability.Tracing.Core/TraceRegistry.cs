using System.Collections.Concurrent;

namespace JonjubNet.Observability.Tracing.Core
{
    /// <summary>
    /// Registro central de traces/spans
    /// Thread-safe usando ConcurrentQueue para almacenar spans
    /// Similar a LogRegistry y MetricRegistry pero para traces
    /// </summary>
    public class TraceRegistry
    {
        private readonly ConcurrentQueue<Span> _spans = new();
        private volatile int _maxSize = 10000; // Tamaño máximo del buffer

        /// <summary>
        /// Tamaño máximo del buffer de spans
        /// Thread-safe: usa volatile para lectura/escritura atómica
        /// </summary>
        public int MaxSize
        {
            get => _maxSize;
            set
            {
                _maxSize = value;
                // Si el tamaño actual excede el nuevo máximo, limpiar los más antiguos
                // ConcurrentQueue.TryDequeue es thread-safe, no requiere lock
                while (_spans.Count > _maxSize)
                {
                    _spans.TryDequeue(out _);
                }
            }
        }

        /// <summary>
        /// Agrega un span al registry
        /// </summary>
        public void AddSpan(Span span)
        {
            if (span == null)
                return;

            // Si el buffer está lleno, eliminar el más antiguo
            if (_spans.Count >= _maxSize)
            {
                _spans.TryDequeue(out _);
            }

            _spans.Enqueue(span);
        }

        /// <summary>
        /// Obtiene todos los spans y limpia el registry
        /// Thread-safe: usa TryDequeue para evitar race conditions
        /// </summary>
        public IReadOnlyList<Span> GetAllSpansAndClear()
        {
            var spans = new List<Span>();
            
            // Dequeue todos los spans disponibles
            while (_spans.TryDequeue(out var span))
            {
                spans.Add(span);
            }

            return spans;
        }

        /// <summary>
        /// Obtiene todos los spans sin limpiar el registry
        /// Útil para inspección sin afectar el estado
        /// </summary>
        public IReadOnlyList<Span> GetAllSpans()
        {
            return _spans.ToArray();
        }

        /// <summary>
        /// Obtiene la cantidad de spans en el registry
        /// </summary>
        public int Count => _spans.Count;

        /// <summary>
        /// Limpia todos los spans del registry
        /// </summary>
        public void Clear()
        {
            while (_spans.TryDequeue(out _))
            {
                // Vaciar la cola
            }
        }

        /// <summary>
        /// Obtiene spans por trace ID
        /// Optimizado: iteración directa sin LINQ para mejor performance y legibilidad
        /// </summary>
        public IReadOnlyList<Span> GetSpansByTraceId(string traceId)
        {
            var result = new List<Span>();
            foreach (var span in _spans)
            {
                if (span.TraceId == traceId)
                    result.Add(span);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Obtiene spans por operación
        /// Optimizado: iteración directa sin LINQ para mejor performance y legibilidad
        /// </summary>
        public IReadOnlyList<Span> GetSpansByOperation(string operationName)
        {
            var result = new List<Span>();
            foreach (var span in _spans)
            {
                if (span.OperationName == operationName)
                    result.Add(span);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Obtiene spans por status
        /// Optimizado: iteración directa sin LINQ para mejor performance y legibilidad
        /// </summary>
        public IReadOnlyList<Span> GetSpansByStatus(SpanStatus status)
        {
            var result = new List<Span>();
            foreach (var span in _spans)
            {
                if (span.Status == status)
                    result.Add(span);
            }
            return result.ToArray();
        }
    }
}
