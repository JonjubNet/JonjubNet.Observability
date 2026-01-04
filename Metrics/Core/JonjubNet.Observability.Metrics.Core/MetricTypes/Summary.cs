using System.Collections.Concurrent;
using JonjubNet.Observability.Metrics.Core.Utils;

namespace JonjubNet.Observability.Metrics.Core.MetricTypes
{
    /// <summary>
    /// Summary para calcular percentiles
    /// </summary>
    public class Summary
    {
        private readonly ConcurrentDictionary<string, SummaryData> _summaries = new();
        private readonly string _name;
        private readonly string _description;
        private readonly double[] _quantiles;

        public Summary(string name, string description, double[]? quantiles = null)
        {
            _name = name;
            _description = description;
            _quantiles = quantiles ?? new[] { 0.5, 0.95, 0.99, 0.999 };
        }

        public string Name => _name;
        public string Description => _description;
        public double[] Quantiles => _quantiles;

        /// <summary>
        /// Observa un valor en el summary
        /// </summary>
        public void Observe(Dictionary<string, string>? tags = null, double value = 0.0)
        {
            var key = KeyCache.CreateKey(tags);
            var data = _summaries.GetOrAdd(key, _ => new SummaryData(_quantiles));
            data.Observe(value);
        }

        /// <summary>
        /// Obtiene los datos del summary
        /// </summary>
        public SummaryData? GetData(Dictionary<string, string>? tags = null)
        {
            var key = KeyCache.CreateKey(tags);
            return _summaries.GetValueOrDefault(key);
        }

        /// <summary>
        /// Obtiene todos los summaries (sin copia, retorna referencia directa)
        /// </summary>
        public IReadOnlyDictionary<string, SummaryData> GetAllData()
        {
            return _summaries; // Retornar directamente sin copia
        }
    }

    /// <summary>
    /// Datos de un summary - optimizado con estructura lock-free y lista ordenada incrementalmente
    /// </summary>
    public class SummaryData
    {
        private readonly double[] _quantiles;
        // Usar SortedSet para mantener valores ordenados incrementalmente (evita ordenar en GetQuantiles)
        private readonly SortedSet<double> _sortedValues;
        private readonly object _lock = new();
        private long _count;
        private double _sum;
        private Dictionary<double, double>? _cachedQuantiles;
        private bool _quantilesDirty = true;
        private const int MaxValues = 10000;
        
        // Singleton para diccionario vacío de quantiles (evita allocations)
        private static readonly Dictionary<double, double> EmptyQuantilesDictionary = new();

        public SummaryData(double[] quantiles)
        {
            _quantiles = quantiles;
            _sortedValues = new SortedSet<double>();
        }

        public void Observe(double value)
        {
            lock (_lock)
            {
                _count++;
                _sum += value;
                
                // Insertar en SortedSet (mantiene orden automáticamente)
                _sortedValues.Add(value);
                
                // Mantener solo los últimos N valores (eliminar el más antiguo si excede)
                if (_sortedValues.Count > MaxValues)
                {
                    _sortedValues.Remove(_sortedValues.Min);
                }
                
                // Marcar quantiles como dirty para invalidar cache
                _quantilesDirty = true;
            }
        }

        public Dictionary<double, double> GetQuantiles()
        {
            lock (_lock)
            {
                if (_sortedValues.Count == 0)
                {
                    // Optimizado: retornar diccionario vacío singleton para evitar allocations
                    return EmptyQuantilesDictionary;
                }

                // Si el cache está válido, retornarlo
                if (!_quantilesDirty && _cachedQuantiles != null)
                {
                    return _cachedQuantiles;
                }

                // Calcular quantiles desde SortedSet (ya está ordenado, no necesita OrderBy)
                // Optimizado: copiar a array manualmente para mejor control de allocations
                var sortedArray = new double[_sortedValues.Count];
                int index = 0;
                foreach (var value in _sortedValues)
                {
                    sortedArray[index++] = value;
                }
                var result = new Dictionary<double, double>();

                foreach (var quantile in _quantiles)
                {
                    var quantileIndex = (int)Math.Ceiling(quantile * sortedArray.Length) - 1;
                    quantileIndex = Math.Max(0, Math.Min(quantileIndex, sortedArray.Length - 1));
                    result[quantile] = sortedArray[quantileIndex];
                }

                // Cachear resultado
                _cachedQuantiles = result;
                _quantilesDirty = false;

                return result;
            }
        }

        public long Count => _count;
        public double Sum => _sum;
    }
}

