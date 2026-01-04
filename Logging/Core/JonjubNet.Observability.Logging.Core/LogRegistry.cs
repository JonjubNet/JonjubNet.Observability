using System.Collections.Concurrent;

namespace JonjubNet.Observability.Logging.Core
{
    /// <summary>
    /// Registro central de logs
    /// Thread-safe usando ConcurrentQueue para almacenar logs
    /// Similar a MetricRegistry pero para logs
    /// </summary>
    public class LogRegistry
    {
        private readonly ConcurrentQueue<StructuredLogEntry> _logs = new();
        private volatile int _maxSize = 10000; // Tamaño máximo del buffer

        /// <summary>
        /// Tamaño máximo del buffer de logs
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
                while (_logs.Count > _maxSize)
                {
                    _logs.TryDequeue(out _);
                }
            }
        }

        /// <summary>
        /// Agrega un log al registry
        /// </summary>
        public void AddLog(StructuredLogEntry log)
        {
            if (log == null)
                return;

            // Si el buffer está lleno, eliminar el más antiguo
            if (_logs.Count >= _maxSize)
            {
                _logs.TryDequeue(out _);
            }

            _logs.Enqueue(log);
        }

        /// <summary>
        /// Obtiene todos los logs y limpia el registry
        /// Thread-safe: usa TryDequeue para evitar race conditions
        /// </summary>
        public IReadOnlyList<StructuredLogEntry> GetAllLogsAndClear()
        {
            var logs = new List<StructuredLogEntry>();
            
            // Dequeue todos los logs disponibles
            while (_logs.TryDequeue(out var log))
            {
                logs.Add(log);
            }

            return logs;
        }

        /// <summary>
        /// Obtiene todos los logs sin limpiar el registry
        /// Útil para inspección sin afectar el estado
        /// </summary>
        public IReadOnlyList<StructuredLogEntry> GetAllLogs()
        {
            return _logs.ToArray();
        }

        /// <summary>
        /// Obtiene la cantidad de logs en el registry
        /// </summary>
        public int Count => _logs.Count;

        /// <summary>
        /// Limpia todos los logs del registry
        /// </summary>
        public void Clear()
        {
            while (_logs.TryDequeue(out _))
            {
                // Vaciar la cola
            }
        }

        /// <summary>
        /// Obtiene logs por nivel
        /// Optimizado: iteración directa sin LINQ para mejor performance y legibilidad
        /// </summary>
        public IReadOnlyList<StructuredLogEntry> GetLogsByLevel(LogLevel level)
        {
            var result = new List<StructuredLogEntry>();
            foreach (var log in _logs)
            {
                if (log.Level == level)
                    result.Add(log);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Obtiene logs por categoría
        /// Optimizado: iteración directa sin LINQ para mejor performance y legibilidad
        /// </summary>
        public IReadOnlyList<StructuredLogEntry> GetLogsByCategory(string category)
        {
            var result = new List<StructuredLogEntry>();
            foreach (var log in _logs)
            {
                if (log.Category == category)
                    result.Add(log);
            }
            return result.ToArray();
        }
    }
}

