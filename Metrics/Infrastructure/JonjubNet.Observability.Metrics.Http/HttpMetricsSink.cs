using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.Core.Interfaces;
using JonjubNet.Observability.Shared.Security;
using JonjubNet.Observability.Shared.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JonjubNet.Observability.Metrics.Http
{
    /// <summary>
    /// Sink de métricas para HTTP
    /// Exporta métricas desde el Registry a un endpoint HTTP
    /// Similar a HttpLogSink pero para métricas
    /// </summary>
    public class HttpMetricsSink : IMetricsSink
    {
        private readonly HttpOptions _options;
        private readonly ILogger<HttpMetricsSink>? _logger;
        private readonly HttpClient _httpClient;
        private readonly SecureHttpClientFactory? _httpClientFactory;
        private readonly EncryptionService? _encryptionService;
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptionsCache.GetDefault();
        
        // String interning para tipos de métricas (optimización GC)
        private static readonly ConcurrentDictionary<string, string> _metricTypeCache = new();
        private static readonly string CounterType = string.Intern("counter");
        private static readonly string GaugeType = string.Intern("gauge");
        private static readonly string HistogramType = string.Intern("histogram");
        private static readonly string SummaryType = string.Intern("summary");

        public string Name => "Http";
        public bool IsEnabled => _options.Enabled;

        public HttpMetricsSink(
            IOptions<HttpOptions> options,
            ILogger<HttpMetricsSink>? logger = null,
            SecureHttpClientFactory? httpClientFactory = null,
            EncryptionService? encryptionService = null)
        {
            _options = options.Value;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _encryptionService = encryptionService;

            // Crear HttpClient usando SecureHttpClientFactory si está disponible
            _httpClient = _httpClientFactory?.CreateSecureClient(_options.EndpointUrl) ?? new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

            // Agregar headers personalizados
            if (_options.Headers != null)
            {
                foreach (var header in _options.Headers)
                {
                    _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
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
                var metrics = ConvertRegistryToMetrics(registry);

                if (metrics.Count == 0)
                    return;

                // Si hay muchas métricas, enviar en batches
                if (metrics.Count > _options.BatchSize)
                {
                    await SendInBatchesAsync(metrics, cancellationToken);
                }
                else
                {
                    await SendAllAsync(metrics, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exporting metrics to HTTP endpoint");
            }
        }

        /// <summary>
        /// Envía todas las métricas en un solo batch
        /// </summary>
        private async Task SendAllAsync(IReadOnlyList<MetricDocument> metrics, CancellationToken cancellationToken)
        {
            if (metrics.Count == 0)
                return;

            try
            {
                var payload = CreatePayload(metrics);
                var content = new StringContent(payload, Encoding.UTF8, _options.DefaultContentType ?? "application/json");

                var response = await _httpClient.PostAsync(_options.EndpointUrl, content, cancellationToken);
                response.EnsureSuccessStatusCode();

                _logger?.LogDebug("Sent {Count} metrics to HTTP endpoint {Endpoint}", metrics.Count, _options.EndpointUrl);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending metrics batch to HTTP endpoint");
                throw;
            }
        }

        /// <summary>
        /// Envía métricas en múltiples batches
        /// </summary>
        private async Task SendInBatchesAsync(IReadOnlyList<MetricDocument> metrics, CancellationToken cancellationToken)
        {
            var batchSize = _options.BatchSize;
            var totalBatches = (int)Math.Ceiling((double)metrics.Count / batchSize);

            for (int i = 0; i < totalBatches; i++)
            {
                var start = i * batchSize;
                var end = Math.Min(start + batchSize, metrics.Count);
                
                // Crear batch sin LINQ (optimización - iteración directa)
                var batch = new List<MetricDocument>(end - start);
                for (int j = start; j < end; j++)
                {
                    batch.Add(metrics[j]);
                }

                try
                {
                    var payload = CreatePayload(batch);
                    var content = new StringContent(payload, Encoding.UTF8, _options.DefaultContentType ?? "application/json");

                    var response = await _httpClient.PostAsync(_options.EndpointUrl, content, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    _logger?.LogDebug("Sent batch {BatchNumber}/{TotalBatches} ({Count} metrics) to HTTP endpoint {Endpoint}", 
                        i + 1, totalBatches, batch.Count, _options.EndpointUrl);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error sending batch {BatchNumber} to HTTP endpoint", i + 1);
                    // Continuar con el siguiente batch en lugar de fallar todo
                }
            }
        }

        /// <summary>
        /// Convierte el Registry a una lista de documentos de métricas
        /// </summary>
        private List<MetricDocument> ConvertRegistryToMetrics(MetricRegistry registry)
        {
            var metrics = new List<MetricDocument>();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Convertir Counters
            foreach (var counter in registry.GetAllCounters().Values)
            {
                foreach (var (key, value) in counter.GetAllValues())
                {
                    var tags = ParseKey(key);
                    metrics.Add(new MetricDocument
                    {
                        Name = counter.Name,
                        Description = counter.Description,
                        Type = CounterType, // String interning
                        Value = value,
                        Tags = tags,
                        Timestamp = timestamp
                    });
                }
            }

            // Convertir Gauges
            foreach (var gauge in registry.GetAllGauges().Values)
            {
                foreach (var (key, value) in gauge.GetAllValues())
                {
                    var tags = ParseKey(key);
                    metrics.Add(new MetricDocument
                    {
                        Name = gauge.Name,
                        Description = gauge.Description,
                        Type = GaugeType, // String interning
                        Value = value,
                        Tags = tags,
                        Timestamp = timestamp
                    });
                }
            }

            // Convertir Histograms
            foreach (var histogram in registry.GetAllHistograms().Values)
            {
                foreach (var kvp in histogram.GetAllData())
                {
                    var key = kvp.Key;
                    var data = kvp.Value;
                    var tags = ParseKey(key);
                    metrics.Add(new MetricDocument
                    {
                        Name = histogram.Name,
                        Description = histogram.Description,
                        Type = HistogramType, // String interning
                        Value = data.Sum, // Usar Sum como valor representativo
                        Tags = tags,
                        Timestamp = timestamp
                    });
                }
            }

            // Convertir Summaries
            foreach (var summary in registry.GetAllSummaries().Values)
            {
                foreach (var kvp in summary.GetAllData())
                {
                    var key = kvp.Key;
                    var data = kvp.Value;
                    var tags = ParseKey(key);
                    metrics.Add(new MetricDocument
                    {
                        Name = summary.Name,
                        Description = summary.Description,
                        Type = SummaryType, // String interning
                        Value = data.Sum, // Usar Sum como valor representativo
                        Tags = tags,
                        Timestamp = timestamp
                    });
                }
            }

            return metrics;
        }

        /// <summary>
        /// Crea el payload JSON a partir de las métricas
        /// </summary>
        private string CreatePayload(IReadOnlyList<MetricDocument> metrics)
        {
            // Construir payload sin LINQ (optimización)
            var metricsList = new List<object>(metrics.Count);
            foreach (var m in metrics)
            {
                metricsList.Add(new
                {
                    name = m.Name,
                    description = m.Description,
                    type = m.Type,
                    value = m.Value,
                    tags = m.Tags,
                    timestamp = m.Timestamp
                });
            }

            var payload = new
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                count = metrics.Count,
                metrics = metricsList
            };

            var json = JsonSerializer.Serialize(payload, JsonOptions);

            // Si la encriptación está habilitada, encriptar el payload
            if (_options.EncryptPayload && _encryptionService != null)
            {
                var jsonBytes = Encoding.UTF8.GetBytes(json);
                var encryptedBytes = _encryptionService.Encrypt(jsonBytes);
                json = Convert.ToBase64String(encryptedBytes);
            }

            return json;
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

        /// <summary>
        /// Documento de métrica para HTTP
        /// </summary>
        private class MetricDocument
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public double Value { get; set; }
            public Dictionary<string, string> Tags { get; set; } = new();
            public long Timestamp { get; set; }
        }
    }
}

