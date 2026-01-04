using JonjubNet.Observability.Metrics.Core.MetricTypes;

namespace JonjubNet.Observability.Metrics.Core.Interfaces
{
    /// <summary>
    /// Cliente principal para registrar métricas (Fast Path API)
    /// </summary>
    public interface IMetricsClient
    {
        /// <summary>
        /// Crea o obtiene un contador
        /// </summary>
        Counter CreateCounter(string name, string description = "");

        /// <summary>
        /// Crea o obtiene un gauge
        /// </summary>
        Gauge CreateGauge(string name, string description = "");

        /// <summary>
        /// Crea o obtiene un histograma
        /// </summary>
        Histogram CreateHistogram(string name, string description = "", double[]? buckets = null);

        /// <summary>
        /// Crea o obtiene un summary
        /// </summary>
        Summary CreateSummary(string name, string description = "", double[]? quantiles = null);

        /// <summary>
        /// Incrementa un contador (fast path)
        /// </summary>
        void Increment(string name, double value = 1.0, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Establece un gauge (fast path)
        /// </summary>
        void SetGauge(string name, double value, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Observa un valor en un histograma (fast path)
        /// </summary>
        void ObserveHistogram(string name, double value, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Alias para ObserveHistogram (compatibilidad con estándares de la industria: Prometheus, OpenTelemetry)
        /// </summary>
        void RecordHistogram(string name, double value, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Inicia un timer y retorna un IDisposable que lo detiene automáticamente
        /// </summary>
        IDisposable StartTimer(string name, Dictionary<string, string>? tags = null);
    }
}

