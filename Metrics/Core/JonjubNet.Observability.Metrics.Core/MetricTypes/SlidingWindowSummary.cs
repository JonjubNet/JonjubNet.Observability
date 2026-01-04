using System.Collections.Concurrent;
using JonjubNet.Observability.Metrics.Core.Utils;

namespace JonjubNet.Observability.Metrics.Core.MetricTypes
{
    /// <summary>
    /// Summary con ventana deslizante de tiempo
    /// Calcula percentiles solo sobre los valores dentro de la ventana de tiempo
    /// </summary>
    public class SlidingWindowSummary
    {
        private readonly ConcurrentDictionary<string, SlidingWindowSummaryData> _summaries = new();
        private readonly string _name;
        private readonly string _description;
        private readonly double[] _quantiles;
        private readonly TimeSpan _windowSize;

        public SlidingWindowSummary(string name, string description, TimeSpan windowSize, double[]? quantiles = null)
        {
            _name = name;
            _description = description;
            _windowSize = windowSize;
            _quantiles = quantiles ?? new[] { 0.5, 0.95, 0.99, 0.999 };
        }

        public string Name => _name;
        public string Description => _description;
        public double[] Quantiles => _quantiles;
        public TimeSpan WindowSize => _windowSize;

        /// <summary>
        /// Observa un valor en el summary con ventana deslizante
        /// </summary>
        public void Observe(Dictionary<string, string>? tags = null, double value = 0.0)
        {
            var key = KeyCache.CreateKey(tags);
            var data = _summaries.GetOrAdd(key, _ => new SlidingWindowSummaryData(_quantiles, _windowSize));
            data.Observe(value);
        }

        /// <summary>
        /// Obtiene los datos del summary
        /// </summary>
        public SlidingWindowSummaryData? GetData(Dictionary<string, string>? tags = null)
        {
            var key = KeyCache.CreateKey(tags);
            return _summaries.GetValueOrDefault(key);
        }

        /// <summary>
        /// Obtiene todos los summaries (sin copia, retorna referencia directa)
        /// </summary>
        public IReadOnlyDictionary<string, SlidingWindowSummaryData> GetAllData()
        {
            return _summaries;
        }
    }

    /// <summary>
    /// Datos de un summary con ventana deslizante
    /// </summary>
    public class SlidingWindowSummaryData
    {
        private readonly double[] _quantiles;
        private readonly SlidingWindow _window;
        private readonly object _lock = new();
        private Dictionary<double, double>? _cachedQuantiles;
        private bool _quantilesDirty = true;
        private DateTime _lastCalculation = DateTime.MinValue;
        private readonly TimeSpan _cacheValidity = TimeSpan.FromSeconds(1);
        
        // Singleton para diccionario vacío de quantiles (evita allocations)
        private static readonly Dictionary<double, double> EmptyQuantilesDictionary = new();

        public SlidingWindowSummaryData(double[] quantiles, TimeSpan windowSize)
        {
            _quantiles = quantiles;
            _window = new SlidingWindow(windowSize);
        }

        public void Observe(double value)
        {
            _window.Add(value);
            _quantilesDirty = true;
            
            // Invalidar cache de valores de ventana
            lock (_lock)
            {
                _cachedWindowValues = null;
            }
        }

        public Dictionary<double, double> GetQuantiles()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                
                // Si el cache es válido y no está dirty, retornarlo
                if (!_quantilesDirty && _cachedQuantiles != null && 
                    now - _lastCalculation < _cacheValidity)
                {
                    return _cachedQuantiles;
                }

                var values = _window.GetValues();
                if (values.Count == 0)
                {
                    // Optimizado: usar diccionario vacío singleton para evitar allocations
                    _cachedQuantiles = EmptyQuantilesDictionary;
                    _quantilesDirty = false;
                    _lastCalculation = now;
                    return _cachedQuantiles;
                }

                // Calcular quantiles desde valores en la ventana
                // Optimizado: usar Array.Sort en lugar de OrderBy().ToArray() para reducir allocations
                var sortedArray = new double[values.Count];
                for (int i = 0; i < values.Count; i++)
                {
                    sortedArray[i] = values[i];
                }
                Array.Sort(sortedArray);
                
                var result = new Dictionary<double, double>();

                foreach (var quantile in _quantiles)
                {
                    var index = (int)Math.Ceiling(quantile * sortedArray.Length) - 1;
                    index = Math.Max(0, Math.Min(index, sortedArray.Length - 1));
                    result[quantile] = sortedArray[index];
                }

                _cachedQuantiles = result;
                _quantilesDirty = false;
                _lastCalculation = now;

                return result;
            }
        }

        // Cache de valores para evitar múltiples llamadas a GetValues()
        private List<double>? _cachedWindowValues;
        private DateTime _lastWindowValuesCacheTime = DateTime.MinValue;
        private readonly TimeSpan _windowValuesCacheValidity = TimeSpan.FromMilliseconds(100);

        private List<double> GetCachedWindowValues()
        {
            var now = DateTime.UtcNow;
            if (_cachedWindowValues != null && now - _lastWindowValuesCacheTime < _windowValuesCacheValidity)
            {
                return _cachedWindowValues;
            }

            // Optimizado: reutilizar lista existente o crear nueva
            var windowValues = _window.GetValues();
            if (_cachedWindowValues == null)
            {
                _cachedWindowValues = new List<double>(windowValues.Count);
            }
            else
            {
                _cachedWindowValues.Clear();
                if (_cachedWindowValues.Capacity < windowValues.Count)
                {
                    _cachedWindowValues.Capacity = windowValues.Count;
                }
            }
            
            foreach (var value in windowValues)
            {
                _cachedWindowValues.Add(value);
            }
            
            _lastWindowValuesCacheTime = now;
            return _cachedWindowValues;
        }

        public long Count => GetCachedWindowValues().Count;
        public double Sum => GetCachedWindowValues().Sum();
        public double Average
        {
            get
            {
                var values = GetCachedWindowValues();
                return values.Count > 0 ? values.Sum() / values.Count : 0;
            }
        }
        public double? Min
        {
            get
            {
                var values = GetCachedWindowValues();
                return values.Count > 0 ? values.Min() : null;
            }
        }
        public double? Max
        {
            get
            {
                var values = GetCachedWindowValues();
                return values.Count > 0 ? values.Max() : null;
            }
        }
    }
}

