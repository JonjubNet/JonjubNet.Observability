using System.Collections.Concurrent;

namespace JonjubNet.Observability.Metrics.Core.Utils
{
    /// <summary>
    /// Ventana deslizante de tiempo para métricas
    /// Mantiene solo los valores dentro de un período de tiempo específico
    /// </summary>
    public class SlidingWindow
    {
        private readonly ConcurrentQueue<TimestampedValue> _values = new();
        private readonly TimeSpan _windowSize;
        private readonly object _cleanupLock = new();
        private DateTime _lastCleanup = DateTime.UtcNow;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromSeconds(10);
        
        // Cache de valores para evitar recálculos costosos
        private List<double>? _cachedValues;
        private DateTime _lastCacheTime = DateTime.MinValue;
        private readonly TimeSpan _cacheValidity = TimeSpan.FromMilliseconds(100);
        private readonly object _cacheLock = new();
        
        // Control de cleanup para evitar demasiados tasks
        private int _pendingCleanups = 0;

        /// <summary>
        /// Crea una nueva ventana deslizante
        /// </summary>
        /// <param name="windowSize">Tamaño de la ventana (ej: TimeSpan.FromMinutes(5))</param>
        public SlidingWindow(TimeSpan windowSize)
        {
            _windowSize = windowSize;
        }

        /// <summary>
        /// Agrega un valor a la ventana con el timestamp actual
        /// </summary>
        public void Add(double value)
        {
            Add(value, DateTime.UtcNow);
        }

        /// <summary>
        /// Agrega un valor a la ventana con un timestamp específico
        /// </summary>
        public void Add(double value, DateTime timestamp)
        {
            _values.Enqueue(new TimestampedValue(value, timestamp));
            
            // Invalidar cache
            lock (_cacheLock)
            {
                _cachedValues = null;
            }
            
            // Limpieza periódica (no bloqueante) - solo si no hay uno pendiente
            var now = DateTime.UtcNow;
            if (now - _lastCleanup >= _cleanupInterval)
            {
                if (Interlocked.CompareExchange(ref _pendingCleanups, 1, 0) == 0)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            Cleanup(now);
                        }
                        finally
                        {
                            Interlocked.Exchange(ref _pendingCleanups, 0);
                        }
                    });
                }
            }
        }

        /// <summary>
        /// Obtiene todos los valores dentro de la ventana actual
        /// Optimizado con cache para evitar recálculos costosos
        /// </summary>
        public IReadOnlyList<double> GetValues()
        {
            var now = DateTime.UtcNow;
            
            // Verificar cache válido
            lock (_cacheLock)
            {
                if (_cachedValues != null && now - _lastCacheTime < _cacheValidity)
                {
                    return _cachedValues;
                }
            }
            
            var cutoff = now - _windowSize;
            Cleanup(cutoff);
            
            // Calcular valores (optimizado - reutilizar lista del cache si existe)
            List<double> result;
            lock (_cacheLock)
            {
                if (_cachedValues == null)
                {
                    _cachedValues = new List<double>(_values.Count);
                }
                else
                {
                    _cachedValues.Clear();
                    // Ajustar capacidad si es necesario
                    if (_cachedValues.Capacity < _values.Count)
                    {
                        _cachedValues.Capacity = _values.Count;
                    }
                }
                
                foreach (var value in _values)
                {
                    if (value.Timestamp >= cutoff)
                    {
                        _cachedValues.Add(value.Value);
                    }
                }
                
                _lastCacheTime = now;
                result = _cachedValues;
            }
            
            return result;
        }

        /// <summary>
        /// Obtiene el número de valores en la ventana
        /// Optimizado usando cache
        /// </summary>
        public int Count => GetValues().Count;

        /// <summary>
        /// Limpia valores fuera de la ventana
        /// </summary>
        private void Cleanup(DateTime cutoff)
        {
            // Solo un thread puede hacer cleanup a la vez
            if (!Monitor.TryEnter(_cleanupLock))
                return;

            try
            {
                _lastCleanup = DateTime.UtcNow;
                
                // Remover valores antiguos
                while (_values.TryPeek(out var value) && value.Timestamp < cutoff)
                {
                    _values.TryDequeue(out _);
                }
            }
            finally
            {
                Monitor.Exit(_cleanupLock);
            }
        }

        /// <summary>
        /// Limpia todos los valores
        /// </summary>
        public void Clear()
        {
            while (_values.TryDequeue(out _)) { }
            
            lock (_cacheLock)
            {
                _cachedValues = null;
            }
        }

        /// <summary>
        /// Valor con timestamp
        /// </summary>
        private readonly record struct TimestampedValue(double Value, DateTime Timestamp);
    }
}

