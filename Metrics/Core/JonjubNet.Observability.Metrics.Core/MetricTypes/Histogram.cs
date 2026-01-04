using System.Collections.Concurrent;
using JonjubNet.Observability.Metrics.Core.Utils;

namespace JonjubNet.Observability.Metrics.Core.MetricTypes
{
    /// <summary>
    /// Histograma con buckets configurables
    /// </summary>
    public class Histogram
    {
        private readonly ConcurrentDictionary<string, HistogramData> _histograms = new();
        private readonly string _name;
        private readonly string _description;
        private readonly double[] _buckets;

        public Histogram(string name, string description, double[]? buckets = null)
        {
            _name = name;
            _description = description;
            _buckets = buckets ?? new[] { 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10 };
        }

        public string Name => _name;
        public string Description => _description;
        public double[] Buckets => _buckets;

        /// <summary>
        /// Observa un valor en el histograma
        /// </summary>
        public void Observe(Dictionary<string, string>? tags = null, double value = 0.0)
        {
            var key = KeyCache.CreateKey(tags);
            var data = _histograms.GetOrAdd(key, _ => new HistogramData(_buckets));
            data.Observe(value);
        }

        /// <summary>
        /// Obtiene los datos del histograma
        /// </summary>
        public HistogramData? GetData(Dictionary<string, string>? tags = null)
        {
            var key = KeyCache.CreateKey(tags);
            return _histograms.GetValueOrDefault(key);
        }

        /// <summary>
        /// Obtiene todos los histogramas (sin copia, retorna referencia directa)
        /// </summary>
        public IReadOnlyDictionary<string, HistogramData> GetAllData()
        {
            return _histograms; // Retornar directamente sin copia
        }
    }

    /// <summary>
    /// Datos de un histograma
    /// </summary>
    public class HistogramData
    {
        private readonly double[] _buckets;
        private readonly long[] _bucketCounts;
        private long _count;
        private double _sum;

        public HistogramData(double[] buckets)
        {
            _buckets = buckets;
            _bucketCounts = new long[buckets.Length];
        }

        public void Observe(double value)
        {
            _count++;
            _sum += value;

            // Usar binary search para encontrar el bucket correcto (O(log n) en lugar de O(n))
            var bucketIndex = FindBucketIndex(value);
            if (bucketIndex >= 0)
            {
                Interlocked.Increment(ref _bucketCounts[bucketIndex]);
            }
        }

        /// <summary>
        /// Encuentra el índice del bucket usando binary search
        /// </summary>
        private int FindBucketIndex(double value)
        {
            int left = 0;
            int right = _buckets.Length - 1;
            int result = -1;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                
                if (_buckets[mid] >= value)
                {
                    result = mid;
                    right = mid - 1; // Buscar en la mitad izquierda
                }
                else
                {
                    left = mid + 1; // Buscar en la mitad derecha
                }
            }

            return result;
        }

        public long Count => _count;
        public double Sum => _sum;
        public double[] Buckets => _buckets;
        public long[] BucketCounts => _bucketCounts;
    }
}

