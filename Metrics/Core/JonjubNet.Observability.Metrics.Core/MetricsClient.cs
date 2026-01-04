using JonjubNet.Observability.Metrics.Core.Interfaces;
using JonjubNet.Observability.Metrics.Core.MetricTypes;
using JonjubNet.Observability.Metrics.Core.Aggregation;
using JonjubNet.Observability.Shared.Context;

namespace JonjubNet.Observability.Metrics.Core
{
    /// <summary>
    /// Implementación del cliente de métricas (Fast Path)
    /// Optimizado: Solo escribe al Registry - todos los sinks leen del Registry
    /// </summary>
    public class MetricsClient : IMetricsClient
    {
        private readonly MetricRegistry _registry;

        public MetricsClient(MetricRegistry registry)
        {
            _registry = registry;
        }

        public Counter CreateCounter(string name, string description = "")
        {
            return _registry.GetOrCreateCounter(name, description);
        }

        public Gauge CreateGauge(string name, string description = "")
        {
            return _registry.GetOrCreateGauge(name, description);
        }

        public Histogram CreateHistogram(string name, string description = "", double[]? buckets = null)
        {
            return _registry.GetOrCreateHistogram(name, description, buckets);
        }

        public Summary CreateSummary(string name, string description = "", double[]? quantiles = null)
        {
            return _registry.GetOrCreateSummary(name, description, quantiles);
        }

        public void Increment(string name, double value = 1.0, Dictionary<string, string>? tags = null)
        {
            // SOLO escritura al Registry - todos los sinks leen del Registry
            var counter = CreateCounter(name);
            var enrichedTags = EnrichTagsWithContext(tags);
            counter.Inc(enrichedTags, value);
        }

        public void SetGauge(string name, double value, Dictionary<string, string>? tags = null)
        {
            // SOLO escritura al Registry - todos los sinks leen del Registry
            var gauge = CreateGauge(name);
            var enrichedTags = EnrichTagsWithContext(tags);
            gauge.Set(enrichedTags, value);
        }

        public void ObserveHistogram(string name, double value, Dictionary<string, string>? tags = null)
        {
            // SOLO escritura al Registry - todos los sinks leen del Registry
            var histogram = CreateHistogram(name);
            var enrichedTags = EnrichTagsWithContext(tags);
            histogram.Observe(enrichedTags, value);
        }

        /// <summary>
        /// Alias para ObserveHistogram (compatibilidad con estándares de la industria)
        /// </summary>
        public void RecordHistogram(string name, double value, Dictionary<string, string>? tags = null)
        {
            ObserveHistogram(name, value, tags); // Delegar a ObserveHistogram
        }

        public IDisposable StartTimer(string name, Dictionary<string, string>? tags = null)
        {
            var histogram = CreateHistogram(name);
            var enrichedTags = EnrichTagsWithContext(tags);
            return TimerMetric.Start(histogram, enrichedTags);
        }

        /// <summary>
        /// Crea o obtiene un summary con ventana deslizante
        /// </summary>
        public SlidingWindowSummary CreateSlidingWindowSummary(
            string name, 
            string description, 
            TimeSpan windowSize, 
            double[]? quantiles = null)
        {
            return _registry.GetOrCreateSlidingWindowSummary(name, description, windowSize, quantiles);
        }

        /// <summary>
        /// Observa un valor en un summary con ventana deslizante
        /// </summary>
        public void ObserveSlidingWindowSummary(
            string name, 
            TimeSpan windowSize, 
            double value, 
            Dictionary<string, string>? tags = null)
        {
            var summary = CreateSlidingWindowSummary(name, "", windowSize);
            var enrichedTags = EnrichTagsWithContext(tags);
            summary.Observe(enrichedTags, value);
        }

        /// <summary>
        /// Agrega un valor al agregador de métricas
        /// </summary>
        public void AddToAggregator(string metricName, double value, Dictionary<string, string>? tags = null)
        {
            _registry.Aggregator.AddValue(metricName, value, tags);
        }

        /// <summary>
        /// Obtiene el valor agregado de una métrica
        /// </summary>
        public double? GetAggregatedValue(
            string metricName, 
            AggregationType aggregationType, 
            Dictionary<string, string>? tags = null)
        {
            return _registry.Aggregator.GetAggregatedValue(metricName, aggregationType, tags);
        }

        /// <summary>
        /// Obtiene estadísticas completas de una métrica agregada
        /// </summary>
        public AggregatedMetricStats? GetAggregatedStats(string metricName, Dictionary<string, string>? tags = null)
        {
            return _registry.Aggregator.GetStats(metricName, tags);
        }

        /// <summary>
        /// Enriquece tags con CorrelationId del contexto de observabilidad
        /// Mejores prácticas: CorrelationId es el identificador único de la transacción
        /// Optimizado: solo crea nuevo Dictionary si hay contexto y tags no están vacíos
        /// </summary>
        private Dictionary<string, string>? EnrichTagsWithContext(Dictionary<string, string>? tags)
        {
            var context = ObservabilityContext.Current;
            if (context == null)
                return tags; // No hay contexto, retornar tags originales

            // Mejores prácticas: CorrelationId siempre debe estar disponible
            // Si no hay CorrelationId en el contexto, no enriquecer (evitar tags vacíos)
            if (string.IsNullOrEmpty(context.CorrelationId))
                return tags;

            // Crear o copiar tags para no mutar el original
            if (tags == null || tags.Count == 0)
            {
                tags = new Dictionary<string, string>();
            }
            else
            {
                tags = new Dictionary<string, string>(tags);
            }

            // Agregar CorrelationId como identificador principal (solo si no existe ya)
            if (!tags.ContainsKey("correlation.id"))
            {
                tags["correlation.id"] = context.CorrelationId;
            }

            // Agregar SpanId y TraceId para correlación de spans (tracing distribuido)
            // Estos son opcionales y solo se agregan si existen
            if (!string.IsNullOrEmpty(context.SpanId) && !tags.ContainsKey("span.id"))
            {
                tags["span.id"] = context.SpanId;
            }
            if (!string.IsNullOrEmpty(context.TraceId) && !tags.ContainsKey("trace.id"))
            {
                tags["trace.id"] = context.TraceId;
            }

            return tags;
        }
    }
}
