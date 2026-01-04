using System.Collections.Concurrent;
using JonjubNet.Observability.Metrics.Core.Utils;

namespace JonjubNet.Observability.Metrics.Core.Aggregation
{
    /// <summary>
    /// Tipo de agregación
    /// </summary>
    public enum AggregationType
    {
        Sum,
        Average,
        Min,
        Max,
        Count,
        Last
    }

    /// <summary>
    /// Agregador de métricas en tiempo real
    /// Permite agregar métricas de múltiples fuentes y calcular agregaciones
    /// </summary>
    public class MetricAggregator
    {
        private readonly ConcurrentDictionary<string, AggregatedMetric> _metrics = new();
        private readonly object _lock = new();

        /// <summary>
        /// Agrega un valor a una métrica
        /// </summary>
        public void AddValue(string metricName, double value, Dictionary<string, string>? tags = null)
        {
            var key = CreateKey(metricName, tags);
            var metric = _metrics.GetOrAdd(key, _ => new AggregatedMetric(metricName, tags));
            
            lock (metric)
            {
                metric.AddValue(value);
            }
        }

        /// <summary>
        /// Obtiene el valor agregado de una métrica
        /// </summary>
        public double? GetAggregatedValue(string metricName, AggregationType aggregationType, Dictionary<string, string>? tags = null)
        {
            var key = CreateKey(metricName, tags);
            if (!_metrics.TryGetValue(key, out var metric))
                return null;

            lock (metric)
            {
                return aggregationType switch
                {
                    AggregationType.Sum => metric.Sum,
                    AggregationType.Average => metric.Count > 0 ? metric.Sum / metric.Count : null,
                    AggregationType.Min => metric.Min,
                    AggregationType.Max => metric.Max,
                    AggregationType.Count => metric.Count,
                    AggregationType.Last => metric.LastValue,
                    _ => null
                };
            }
        }

        /// <summary>
        /// Obtiene estadísticas completas de una métrica
        /// </summary>
        public AggregatedMetricStats? GetStats(string metricName, Dictionary<string, string>? tags = null)
        {
            var key = CreateKey(metricName, tags);
            if (!_metrics.TryGetValue(key, out var metric))
                return null;

            lock (metric)
            {
                return new AggregatedMetricStats
                {
                    Name = metric.Name,
                    Tags = metric.Tags,
                    Count = metric.Count,
                    Sum = metric.Sum,
                    Average = metric.Count > 0 ? metric.Sum / metric.Count : 0,
                    Min = metric.Min,
                    Max = metric.Max,
                    LastValue = metric.LastValue,
                    FirstTimestamp = metric.FirstTimestamp,
                    LastTimestamp = metric.LastTimestamp
                };
            }
        }

        /// <summary>
        /// Obtiene todas las métricas agregadas
        /// Optimizado: pre-alloca capacidad del diccionario
        /// </summary>
        public IReadOnlyDictionary<string, AggregatedMetricStats> GetAllStats()
        {
            // Pre-allocar capacidad para reducir reallocations
            var result = new Dictionary<string, AggregatedMetricStats>(_metrics.Count);
            
            foreach (var kvp in _metrics)
            {
                var metric = kvp.Value;
                lock (metric)
                {
                    result[kvp.Key] = new AggregatedMetricStats
                    {
                        Name = metric.Name,
                        Tags = metric.Tags,
                        Count = metric.Count,
                        Sum = metric.Sum,
                        Average = metric.Count > 0 ? metric.Sum / metric.Count : 0,
                        Min = metric.Min,
                        Max = metric.Max,
                        LastValue = metric.LastValue,
                        FirstTimestamp = metric.FirstTimestamp,
                        LastTimestamp = metric.LastTimestamp
                    };
                }
            }
            
            return result;
        }

        /// <summary>
        /// Limpia todas las métricas
        /// </summary>
        public void Clear()
        {
            _metrics.Clear();
        }

        /// <summary>
        /// Limpia una métrica específica
        /// </summary>
        public bool Remove(string metricName, Dictionary<string, string>? tags = null)
        {
            var key = CreateKey(metricName, tags);
            return _metrics.TryRemove(key, out _);
        }

        private static string CreateKey(string metricName, Dictionary<string, string>? tags)
        {
            if (tags == null || tags.Count == 0)
                return metricName;

            // Usar KeyCache para optimizar creación de keys (reduce allocations y OrderBy)
            var tagKey = KeyCache.CreateKey(tags);
            if (string.IsNullOrEmpty(tagKey))
                return metricName;

            // Combinar metricName con tagKey (optimizado - solo 1 allocation)
            return $"{metricName}|{tagKey}";
        }

        /// <summary>
        /// Métrica agregada interna
        /// </summary>
        private class AggregatedMetric
        {
            public string Name { get; }
            public Dictionary<string, string>? Tags { get; }
            public long Count { get; private set; }
            public double Sum { get; private set; }
            public double? Min { get; private set; }
            public double? Max { get; private set; }
            public double? LastValue { get; private set; }
            public DateTime? FirstTimestamp { get; private set; }
            public DateTime? LastTimestamp { get; private set; }

            public AggregatedMetric(string name, Dictionary<string, string>? tags)
            {
                Name = name;
                Tags = tags;
            }

            public void AddValue(double value)
            {
                Count++;
                Sum += value;
                
                if (Min == null || value < Min)
                    Min = value;
                
                if (Max == null || value > Max)
                    Max = value;
                
                LastValue = value;
                
                var now = DateTime.UtcNow;
                if (FirstTimestamp == null)
                    FirstTimestamp = now;
                LastTimestamp = now;
            }
        }
    }

    /// <summary>
    /// Estadísticas de una métrica agregada
    /// </summary>
    public class AggregatedMetricStats
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, string>? Tags { get; set; }
        public long Count { get; set; }
        public double Sum { get; set; }
        public double Average { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public double? LastValue { get; set; }
        public DateTime? FirstTimestamp { get; set; }
        public DateTime? LastTimestamp { get; set; }
    }
}

