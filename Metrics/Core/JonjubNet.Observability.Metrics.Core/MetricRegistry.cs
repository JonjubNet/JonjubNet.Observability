using System.Collections.Concurrent;
using JonjubNet.Observability.Metrics.Core.MetricTypes;
using JonjubNet.Observability.Metrics.Core.Aggregation;

namespace JonjubNet.Observability.Metrics.Core
{
    /// <summary>
    /// Registro central de métricas
    /// Thread-safe usando ConcurrentDictionary
    /// </summary>
    public class MetricRegistry
    {
        private readonly ConcurrentDictionary<string, Counter> _counters = new();
        private readonly ConcurrentDictionary<string, Gauge> _gauges = new();
        private readonly ConcurrentDictionary<string, Histogram> _histograms = new();
        private readonly ConcurrentDictionary<string, Summary> _summaries = new();
        private readonly ConcurrentDictionary<string, SlidingWindowSummary> _slidingWindowSummaries = new();
        private readonly MetricAggregator _aggregator = new();

        /// <summary>
        /// Crea o obtiene un contador
        /// </summary>
        public Counter GetOrCreateCounter(string name, string description)
        {
            return _counters.GetOrAdd(name, _ => new Counter(name, description));
        }

        /// <summary>
        /// Crea o obtiene un gauge
        /// </summary>
        public Gauge GetOrCreateGauge(string name, string description)
        {
            return _gauges.GetOrAdd(name, _ => new Gauge(name, description));
        }

        /// <summary>
        /// Crea o obtiene un histograma
        /// </summary>
        public Histogram GetOrCreateHistogram(string name, string description, double[]? buckets = null)
        {
            return _histograms.GetOrAdd(name, _ => new Histogram(name, description, buckets));
        }

        /// <summary>
        /// Crea o obtiene un summary
        /// </summary>
        public Summary GetOrCreateSummary(string name, string description, double[]? quantiles = null)
        {
            return _summaries.GetOrAdd(name, _ => new Summary(name, description, quantiles));
        }

        /// <summary>
        /// Obtiene todos los contadores
        /// </summary>
        public IReadOnlyDictionary<string, Counter> GetAllCounters() => _counters;

        /// <summary>
        /// Obtiene todos los gauges
        /// </summary>
        public IReadOnlyDictionary<string, Gauge> GetAllGauges() => _gauges;

        /// <summary>
        /// Obtiene todos los histogramas
        /// </summary>
        public IReadOnlyDictionary<string, Histogram> GetAllHistograms() => _histograms;

        /// <summary>
        /// Obtiene todos los summaries
        /// </summary>
        public IReadOnlyDictionary<string, Summary> GetAllSummaries() => _summaries;

        /// <summary>
        /// Crea o obtiene un summary con ventana deslizante
        /// </summary>
        public SlidingWindowSummary GetOrCreateSlidingWindowSummary(
            string name, 
            string description, 
            TimeSpan windowSize, 
            double[]? quantiles = null)
        {
            return _slidingWindowSummaries.GetOrAdd(
                name, 
                _ => new SlidingWindowSummary(name, description, windowSize, quantiles));
        }

        /// <summary>
        /// Obtiene todos los summaries con ventana deslizante
        /// </summary>
        public IReadOnlyDictionary<string, SlidingWindowSummary> GetAllSlidingWindowSummaries() => _slidingWindowSummaries;

        /// <summary>
        /// Obtiene el agregador de métricas
        /// </summary>
        public MetricAggregator Aggregator => _aggregator;

        /// <summary>
        /// Limpia todas las métricas
        /// </summary>
        public void Clear()
        {
            _counters.Clear();
            _gauges.Clear();
            _histograms.Clear();
            _summaries.Clear();
            _slidingWindowSummaries.Clear();
            _aggregator.Clear();
        }
    }
}

