using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.IO.Compression;
using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.Core.Interfaces;
using JonjubNet.Observability.Shared.Utils;
using JonjubNet.Observability.Shared.OpenTelemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using JonjubNet.Observability.Shared.Security;

namespace JonjubNet.Observability.Metrics.OpenTelemetry
{
    /// <summary>
    /// Exporter de métricas para OpenTelemetry Collector
    /// </summary>
    public class OTLPExporter : IMetricsSink
    {
        private readonly OTLOptions _options;
        private readonly ILogger<OTLPExporter>? _logger;
        private readonly HttpClient? _httpClient;
        private readonly EncryptionService? _encryptionService;
        private readonly bool _encryptInTransit;
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptionsCache.GetDefault();

        public string Name => "OpenTelemetry";
        public bool IsEnabled => _options.Enabled;

        public OTLPExporter(
            IOptions<OTLOptions> options,
            ILogger<OTLPExporter>? logger = null,
            HttpClient? httpClient = null,
            EncryptionService? encryptionService = null,
            SecureHttpClientFactory? secureHttpClientFactory = null,
            bool encryptInTransit = false,
            bool enableTls = true)
        {
            _options = options.Value;
            _logger = logger;
            _encryptionService = encryptionService;
            _encryptInTransit = encryptInTransit;
            
            // Usar helper compartido para crear HttpClient
            _httpClient = OtlpHttpClientHelper.CreateHttpClient(
                _options.Enabled,
                _options.Endpoint,
                _options.TimeoutSeconds,
                secureHttpClientFactory,
                httpClient,
                enableTls,
                logger);
        }

        /// <summary>
        /// Exporta métricas desde el Registry (método principal optimizado)
        /// </summary>
        public async ValueTask ExportFromRegistryAsync(MetricRegistry registry, CancellationToken cancellationToken = default)
        {
            // Verificar si está habilitado - si no, retornar inmediatamente sin hacer nada
            if (!_options.Enabled)
                return;
            
            // Verificar que HttpClient esté disponible (debería estar si Enabled = true)
            if (_httpClient == null)
            {
                _logger?.LogWarning("OpenTelemetry is enabled but HttpClient is not available. Skipping export.");
                return;
            }

            try
            {
                var otlpPayload = ConvertRegistryToOTLPFormat(registry);
                var url = OtlpUrlBuilder.BuildUrl(_options.Endpoint, _options.Protocol, "metrics");
                
                // Usar helper compartido para crear HttpContent
                var content = OtlpContentBuilder.CreateContent(
                    otlpPayload,
                    _options.EnableCompression,
                    _encryptionService,
                    _encryptInTransit,
                    _logger);
                
                var response = await _httpClient!.PostAsync(url, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger?.LogWarning("OTLP export failed with status {StatusCode}: {Error}", 
                        response.StatusCode, errorContent);
                }
                else
                {
                    _logger?.LogDebug("Exported metrics to OTLP endpoint {Endpoint}", _options.Endpoint);
                }
            }
            catch (HttpRequestException httpEx) when (httpEx.InnerException is System.Net.Sockets.SocketException)
            {
                // Conexión rechazada o endpoint no disponible - solo loguear como warning
                _logger?.LogWarning("OpenTelemetry endpoint not available: {Endpoint}. Metrics will not be exported to OTLP. To disable, set Metrics:OpenTelemetry:Enabled to false.", _options.Endpoint);
            }
            catch (Exception ex)
            {
                // Otros errores (timeout, formato, etc.) - loguear como error
                _logger?.LogError(ex, "Error exporting metrics to OTLP endpoint {Endpoint}", _options.Endpoint);
            }
        }

        private object ConvertRegistryToOTLPFormat(MetricRegistry registry)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000;
            var metrics = new List<object>();

            // Convertir Counters
            foreach (var counter in registry.GetAllCounters().Values)
            {
                foreach (var (key, value) in counter.GetAllValues())
                {
                    var tags = ParseKey(key);
                    metrics.Add(new
                    {
                        name = counter.Name,
                        description = counter.Description,
                        unit = "",
                        data = new
                        {
                            dataPoints = new[]
                            {
                                new
                                {
                                    asInt = (long?)value,
                                    timeUnixNano = timestamp,
                                    attributes = tags.Select(tagKvp => new
                                    {
                                        key = tagKvp.Key,
                                        value = new { stringValue = tagKvp.Value }
                                    }).ToArray()
                                }
                            }
                        }
                    });
                }
            }

            // Convertir Gauges
            foreach (var gauge in registry.GetAllGauges().Values)
            {
                foreach (var (key, value) in gauge.GetAllValues())
                {
                    var tags = ParseKey(key);
                    metrics.Add(new
                    {
                        name = gauge.Name,
                        description = gauge.Description,
                        unit = "",
                        data = new
                        {
                            dataPoints = new[]
                            {
                                new
                                {
                                    asDouble = (double?)value,
                                    timeUnixNano = timestamp,
                                    attributes = tags.Select(tagKvp => new
                                    {
                                        key = tagKvp.Key,
                                        value = new { stringValue = tagKvp.Value }
                                    }).ToArray()
                                }
                            }
                        }
                    });
                }
            }

            // Convertir Histograms
            foreach (var histogram in registry.GetAllHistograms().Values)
            {
                foreach (var (key, data) in histogram.GetAllData())
                {
                    var tags = ParseKey(key);
                    metrics.Add(new
                    {
                        name = histogram.Name,
                        description = histogram.Description,
                        unit = "",
                        data = new
                        {
                            dataPoints = new[]
                            {
                                new
                                {
                                    asDouble = (double?)data.Sum,
                                    timeUnixNano = timestamp,
                                    attributes = tags.Select(tagKvp => new
                                    {
                                        key = tagKvp.Key,
                                        value = new { stringValue = tagKvp.Value }
                                    }).ToArray()
                                }
                            }
                        }
                    });
                }
            }

            // Convertir Summaries
            foreach (var summary in registry.GetAllSummaries().Values)
            {
                foreach (var (key, data) in summary.GetAllData())
                {
                    var tags = ParseKey(key);
                    var quantiles = data.GetQuantiles();
                    foreach (var quantile in quantiles)
                    {
                        metrics.Add(new
                        {
                            name = summary.Name,
                            description = summary.Description,
                            unit = "",
                            data = new
                            {
                                dataPoints = new[]
                                {
                                    new
                                    {
                                        asDouble = (double?)quantile.Value,
                                        timeUnixNano = timestamp,
                                        attributes = tags.Concat(new[] { new KeyValuePair<string, string>("quantile", quantile.Key.ToString()) })
                                            .Select(tagKvp => new
                                            {
                                                key = tagKvp.Key,
                                                value = new { stringValue = tagKvp.Value }
                                            }).ToArray()
                                    }
                                }
                            }
                        });
                    }
                }
            }

            var resourceMetrics = new
            {
                resourceMetrics = new[]
                {
                    new
                    {
                        resource = new { },
                        scopeMetrics = new[]
                        {
                            new
                            {
                                scope = new { },
                                metrics = metrics.ToArray()
                            }
                        }
                    }
                }
            };

            return resourceMetrics;
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
