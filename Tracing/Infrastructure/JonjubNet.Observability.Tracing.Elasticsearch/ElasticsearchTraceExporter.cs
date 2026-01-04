using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using JonjubNet.Observability.Tracing.Core;
using JonjubNet.Observability.Tracing.Core.Interfaces;
using JonjubNet.Observability.Shared.Security;
using JonjubNet.Observability.Shared.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JonjubNet.Observability.Tracing.Elasticsearch
{
    /// <summary>
    /// Exporter de traces para Elasticsearch
    /// Exporta spans desde el Registry a Elasticsearch usando la API de _bulk
    /// Similar a ElasticsearchLogSink pero para traces/spans
    /// </summary>
    public class ElasticsearchTraceExporter : ITraceSink
    {
        private readonly ElasticsearchOptions _options;
        private readonly ILogger<ElasticsearchTraceExporter>? _logger;
        private readonly HttpClient _httpClient;
        private readonly SecureHttpClientFactory? _httpClientFactory;
        private readonly EncryptionService? _encryptionService;
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptionsCache.GetDefault();
        
        // String interning para valores constantes (optimización GC)
        private static readonly string IndexActionType = string.Intern("_doc");

        public string Name => "Elasticsearch";
        public bool IsEnabled => _options.Enabled;

        public ElasticsearchTraceExporter(
            IOptions<ElasticsearchOptions> options,
            ILogger<ElasticsearchTraceExporter>? logger = null,
            SecureHttpClientFactory? httpClientFactory = null,
            EncryptionService? encryptionService = null)
        {
            _options = options.Value;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _encryptionService = encryptionService;

            // Crear HttpClient usando SecureHttpClientFactory si está disponible
            _httpClient = _httpClientFactory?.CreateSecureClient(_options.BaseUrl) ?? new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

            // Agregar autenticación básica si está configurada
            if (!string.IsNullOrEmpty(_options.Username) && !string.IsNullOrEmpty(_options.Password))
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.Username}:{_options.Password}"));
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            }

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
        /// Exporta spans desde el Registry (método principal optimizado)
        /// </summary>
        public async ValueTask ExportFromRegistryAsync(TraceRegistry registry, CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
                return;

            try
            {
                var spans = registry.GetAllSpansAndClear();

                if (spans.Count == 0)
                    return;

                // Elasticsearch _bulk API requiere formato NDJSON
                // Si hay muchos spans, enviar en batches
                if (spans.Count > _options.BatchSize)
                {
                    await SendInBatchesAsync(spans, cancellationToken);
                }
                else
                {
                    await SendAllAsync(spans, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exporting traces to Elasticsearch");
            }
        }

        /// <summary>
        /// Envía todos los spans en un solo batch
        /// </summary>
        private async Task SendAllAsync(IReadOnlyList<Span> spans, CancellationToken cancellationToken)
        {
            if (spans.Count == 0)
                return;

            try
            {
                var payload = CreateBulkPayload(spans);
                var content = new StringContent(payload, Encoding.UTF8, "application/x-ndjson");

                var url = $"{_options.BaseUrl}/{_options.IndexName}/_bulk";
                var response = await _httpClient.PostAsync(url, content, cancellationToken);
                response.EnsureSuccessStatusCode();

                // Contar traces únicos sin LINQ (optimización)
                var uniqueTraces = new HashSet<string>();
                foreach (var span in spans)
                {
                    uniqueTraces.Add(span.TraceId);
                }
                _logger?.LogDebug("Sent {Count} spans ({TraceCount} traces) to Elasticsearch index {Index}", 
                    spans.Count, uniqueTraces.Count, _options.IndexName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending traces batch to Elasticsearch");
                throw;
            }
        }

        /// <summary>
        /// Envía spans en múltiples batches
        /// </summary>
        private async Task SendInBatchesAsync(IReadOnlyList<Span> spans, CancellationToken cancellationToken)
        {
            var batchSize = _options.BatchSize;
            var totalBatches = (int)Math.Ceiling((double)spans.Count / batchSize);

            for (int i = 0; i < totalBatches; i++)
            {
                var start = i * batchSize;
                var end = Math.Min(start + batchSize, spans.Count);
                
                // Crear batch sin LINQ (optimización - iteración directa)
                var batch = new List<Span>(end - start);
                for (int j = start; j < end; j++)
                {
                    batch.Add(spans[j]);
                }

                try
                {
                    var payload = CreateBulkPayload(batch);
                    var content = new StringContent(payload, Encoding.UTF8, "application/x-ndjson");

                    var url = $"{_options.BaseUrl}/{_options.IndexName}/_bulk";
                    var response = await _httpClient.PostAsync(url, content, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    // Contar traces únicos sin LINQ (optimización)
                    var uniqueTraces = new HashSet<string>();
                    foreach (var span in batch)
                    {
                        uniqueTraces.Add(span.TraceId);
                    }
                    _logger?.LogDebug("Sent batch {BatchNumber}/{TotalBatches} ({Count} spans, {TraceCount} traces) to Elasticsearch index {Index}", 
                        i + 1, totalBatches, batch.Count, uniqueTraces.Count, _options.IndexName);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error sending batch {BatchNumber} to Elasticsearch", i + 1);
                    // Continuar con el siguiente batch en lugar de fallar todo
                }
            }
        }

        /// <summary>
        /// Crea el payload NDJSON para la API _bulk de Elasticsearch
        /// Formato: { "index": {} }\n{ span data }\n{ "index": {} }\n{ span data }\n...
        /// </summary>
        private string CreateBulkPayload(IReadOnlyList<Span> spans)
        {
            var sb = new StringBuilder(spans.Count * 1024); // Pre-allocate capacity

            foreach (var span in spans)
            {
                // Action line: { "index": { "_index": "traces", "_type": "_doc" } }
                var indexAction = new
                {
                    index = new
                    {
                        _index = _options.IndexName,
                        _type = string.Intern(_options.DocumentType) // String interning
                    }
                };
                sb.AppendLine(JsonSerializer.Serialize(indexAction, JsonOptions));

                // Document line: el span como JSON
                var document = new
                {
                    spanId = span.SpanId,
                    traceId = span.TraceId,
                    parentSpanId = span.ParentSpanId,
                    operationName = span.OperationName,
                    kind = span.Kind.ToString(),
                    status = span.Status.ToString(),
                    errorMessage = span.ErrorMessage,
                    startTime = span.StartTime.ToUnixTimeMilliseconds(),
                    endTime = span.EndTime?.ToUnixTimeMilliseconds(),
                    durationMs = span.DurationMs,
                    tags = span.Tags,
                    properties = span.Properties,
                    serviceName = span.ServiceName,
                    resourceName = span.ResourceName,
                    isActive = span.IsActive,
                    events = span.Events != null && span.Events.Count > 0
                        ? CreateSpanEvents(span.Events)
                        : null
                };
                sb.AppendLine(JsonSerializer.Serialize(document, JsonOptions));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Crea la lista de eventos del span sin LINQ (optimización)
        /// </summary>
        private List<object> CreateSpanEvents(IReadOnlyList<SpanEvent> events)
        {
            var result = new List<object>(events.Count);
            foreach (var e in events)
            {
                result.Add(new
                {
                    name = e.Name,
                    timestamp = e.Timestamp.ToUnixTimeMilliseconds(),
                    attributes = e.Attributes
                });
            }
            return result;
        }
    }
}

