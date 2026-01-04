using System.Collections.Concurrent;
using JonjubNet.Observability.Metrics.Core.Aggregation;

namespace JonjubNet.Observability.Metrics.Core.Utils
{
    /// <summary>
    /// Pool optimizado para List<T> y Dictionary<TKey, TValue> para reducir allocations
    /// </summary>
    public static class CollectionPool
    {
        private static readonly ConcurrentQueue<List<MetricPoint>> _metricPointListPool = new();
        private static readonly ConcurrentQueue<List<string>> _stringListPool = new();
        private static readonly ConcurrentQueue<List<double>> _doubleListPool = new();
        private static readonly ConcurrentQueue<Dictionary<string, string>> _dictionaryPool = new();
        private static readonly ConcurrentQueue<Dictionary<string, AggregatedMetricStats>> _aggregatedStatsDictionaryPool = new();
        private const int MaxPoolSize = 500;

        /// <summary>
        /// Obtiene una List<MetricPoint> del pool o crea una nueva
        /// </summary>
        public static List<MetricPoint> RentMetricPointList()
        {
            if (_metricPointListPool.TryDequeue(out var list))
            {
                return list;
            }
            return new List<MetricPoint>();
        }

        /// <summary>
        /// Devuelve una List<MetricPoint> al pool
        /// </summary>
        public static void ReturnMetricPointList(List<MetricPoint> list)
        {
            if (list == null) return;
            list.Clear();
            if (_metricPointListPool.Count < MaxPoolSize)
            {
                _metricPointListPool.Enqueue(list);
            }
        }

        /// <summary>
        /// Obtiene una List<string> del pool o crea una nueva
        /// </summary>
        public static List<string> RentStringList()
        {
            if (_stringListPool.TryDequeue(out var list))
            {
                return list;
            }
            return new List<string>();
        }

        /// <summary>
        /// Devuelve una List<string> al pool
        /// </summary>
        public static void ReturnStringList(List<string> list)
        {
            if (list == null) return;
            list.Clear();
            if (_stringListPool.Count < MaxPoolSize)
            {
                _stringListPool.Enqueue(list);
            }
        }

        /// <summary>
        /// Obtiene un Dictionary<string, string> del pool o crea uno nuevo
        /// </summary>
        public static Dictionary<string, string> RentDictionary()
        {
            if (_dictionaryPool.TryDequeue(out var dict))
            {
                return dict;
            }
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Devuelve un Dictionary<string, string> al pool
        /// </summary>
        public static void ReturnDictionary(Dictionary<string, string> dictionary)
        {
            if (dictionary == null) return;
            dictionary.Clear();
            if (_dictionaryPool.Count < MaxPoolSize)
            {
                _dictionaryPool.Enqueue(dictionary);
            }
        }

        /// <summary>
        /// Obtiene una List<double> del pool o crea una nueva
        /// </summary>
        public static List<double> RentDoubleList()
        {
            if (_doubleListPool.TryDequeue(out var list))
            {
                return list;
            }
            return new List<double>();
        }

        /// <summary>
        /// Devuelve una List<double> al pool
        /// </summary>
        public static void ReturnDoubleList(List<double> list)
        {
            if (list == null) return;
            list.Clear();
            if (_doubleListPool.Count < MaxPoolSize)
            {
                _doubleListPool.Enqueue(list);
            }
        }

        /// <summary>
        /// Obtiene un Dictionary<string, AggregatedMetricStats> del pool o crea uno nuevo
        /// </summary>
        public static Dictionary<string, AggregatedMetricStats> RentAggregatedStatsDictionary()
        {
            if (_aggregatedStatsDictionaryPool.TryDequeue(out var dict))
            {
                return dict;
            }
            return new Dictionary<string, AggregatedMetricStats>();
        }

        /// <summary>
        /// Devuelve un Dictionary<string, AggregatedMetricStats> al pool
        /// </summary>
        public static void ReturnAggregatedStatsDictionary(Dictionary<string, AggregatedMetricStats> dictionary)
        {
            if (dictionary == null) return;
            dictionary.Clear();
            if (_aggregatedStatsDictionaryPool.Count < MaxPoolSize)
            {
                _aggregatedStatsDictionaryPool.Enqueue(dictionary);
            }
        }

        /// <summary>
        /// Limpia todos los pools
        /// </summary>
        public static void Clear()
        {
            while (_metricPointListPool.TryDequeue(out _)) { }
            while (_stringListPool.TryDequeue(out _)) { }
            while (_doubleListPool.TryDequeue(out _)) { }
            while (_dictionaryPool.TryDequeue(out _)) { }
            while (_aggregatedStatsDictionaryPool.TryDequeue(out _)) { }
        }
    }
}

