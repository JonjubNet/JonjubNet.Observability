using System.Collections.Concurrent;
using JonjubNet.Observability.Metrics.Core;

namespace JonjubNet.Observability.Metrics.Shared.Utils
{
    /// <summary>
    /// Pool de objetos para MetricPoint para reducir allocations
    /// </summary>
    public static class MetricPointPool
    {
        private static readonly ConcurrentQueue<MetricPoint> _pool = new();
        private const int MaxPoolSize = 1000;

        /// <summary>
        /// Obtiene un MetricPoint del pool o crea uno nuevo
        /// </summary>
        public static MetricPoint Rent(string name, MetricType type, double value, Dictionary<string, string>? tags = null)
        {
            if (_pool.TryDequeue(out var point))
            {
                // Reutilizar el punto del pool
                return new MetricPoint(name, type, value, tags);
            }

            // Crear nuevo punto
            return new MetricPoint(name, type, value, tags);
        }

        /// <summary>
        /// Devuelve un MetricPoint al pool para reutilización
        /// </summary>
        public static void Return(MetricPoint point)
        {
            if (_pool.Count < MaxPoolSize)
            {
                _pool.Enqueue(point);
            }
        }

        /// <summary>
        /// Limpia el pool
        /// </summary>
        public static void Clear()
        {
            while (_pool.TryDequeue(out _)) { }
        }
    }
}
