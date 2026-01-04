using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JonjubNet.Observability.Metrics.Prometheus
{
    /// <summary>
    /// Exporter de métricas para Prometheus
    /// Expone endpoint /metrics en formato Prometheus
    /// </summary>
    public class PrometheusExporter : IMetricsSink
    {
        private readonly MetricRegistry _registry;
        private readonly PrometheusFormatter _formatter;
        private readonly PrometheusOptions _options;
        private readonly ILogger<PrometheusExporter>? _logger;

        public string Name => "Prometheus";
        public bool IsEnabled => _options.Enabled;

        public PrometheusExporter(
            MetricRegistry registry,
            PrometheusFormatter formatter,
            IOptions<PrometheusOptions> options,
            ILogger<PrometheusExporter>? logger = null)
        {
            _registry = registry;
            _formatter = formatter;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Exporta métricas desde el Registry (método principal optimizado)
        /// </summary>
        public ValueTask ExportFromRegistryAsync(MetricRegistry registry, CancellationToken cancellationToken = default)
        {
            // Prometheus lee directamente del Registry - no necesita procesamiento adicional
            // El formato se hace en GetMetricsText() cuando se solicita el endpoint
            return ValueTask.CompletedTask;
        }


        /// <summary>
        /// Obtiene las métricas formateadas en texto Prometheus
        /// </summary>
        public string GetMetricsText()
        {
            return _formatter.FormatRegistry(_registry);
        }
    }
}

