using System.Net;
using System.Net.Sockets;
using System.Text;
using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JonjubNet.Observability.Metrics.StatsD
{
    /// <summary>
    /// Sink de métricas para StatsD
    /// </summary>
    public class StatsDSink : IMetricsSink
    {
        private readonly StatsDOptions _options;
        private readonly ILogger<StatsDSink>? _logger;
        private readonly UdpClient? _udpClient;
        private readonly IPEndPoint? _endPoint;

        public string Name => "StatsD";
        public bool IsEnabled => _options.Enabled;

        public StatsDSink(
            IOptions<StatsDOptions> options,
            ILogger<StatsDSink>? logger = null)
        {
            _options = options.Value;
            _logger = logger;

            if (_options.Enabled)
            {
                try
                {
                    _udpClient = new UdpClient();
                    var hostEntry = Dns.GetHostEntry(_options.Host);
                    _endPoint = new IPEndPoint(hostEntry.AddressList[0], _options.Port);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to initialize StatsD client, will use logging fallback");
                }
            }
        }

        /// <summary>
        /// Exporta métricas desde el Registry (método principal optimizado)
        /// </summary>
        public async ValueTask ExportFromRegistryAsync(MetricRegistry registry, CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
                return;

            try
            {
                var sb = new StringBuilder(1024); // Pre-allocate capacity
                var first = true;

                // Convertir Counters
                foreach (var counter in registry.GetAllCounters().Values)
                {
                    foreach (var (key, value) in counter.GetAllValues())
                    {
                        if (!first) sb.Append('\n');
                        var message = FormatFromRegistry(counter.Name, "counter", value, ParseKey(key));
                        if (!string.IsNullOrEmpty(message))
                        {
                            sb.Append(message);
                            first = false;
                        }
                    }
                }

                // Convertir Gauges
                foreach (var gauge in registry.GetAllGauges().Values)
                {
                    foreach (var (key, value) in gauge.GetAllValues())
                    {
                        if (!first) sb.Append('\n');
                        var message = FormatFromRegistry(gauge.Name, "gauge", value, ParseKey(key));
                        if (!string.IsNullOrEmpty(message))
                        {
                            sb.Append(message);
                            first = false;
                        }
                    }
                }

                // Similar para Histograms...

                if (sb.Length > 0)
                {
                    if (_udpClient != null && _endPoint != null)
                    {
                        var data = Encoding.UTF8.GetBytes(sb.ToString());
                        await _udpClient.SendAsync(data, data.Length, _endPoint);
                    }
                    else
                    {
                        _logger?.LogDebug("StatsD (fallback): {Messages}", sb.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exporting metrics to StatsD");
            }
        }

        private string FormatFromRegistry(string name, string type, double value, Dictionary<string, string>? tags)
        {
            var sb = new StringBuilder(64);
            sb.Append(name).Append(':').Append(value);

            string metricTypeSuffix = type switch
            {
                "counter" => "|c",
                "gauge" => "|g",
                "histogram" => "|h",
                "timer" => "|ms",
                _ => "|g"
            };
            sb.Append(metricTypeSuffix);

            if (tags != null && tags.Count > 0)
            {
                sb.Append("|#");
                bool firstTag = true;
                foreach (var kvp in tags)
                {
                    if (!firstTag) sb.Append(',');
                    sb.Append(kvp.Key).Append(':').Append(kvp.Value);
                    firstTag = false;
                }
            }
            return sb.ToString();
        }

        private Dictionary<string, string> ParseKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return new Dictionary<string, string>();

            var result = new Dictionary<string, string>();
            var pairs = key.Split(',');
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=');
                if (parts.Length == 2)
                {
                    result[parts[0]] = parts[1];
                }
            }
            return result;
        }

    }
}
