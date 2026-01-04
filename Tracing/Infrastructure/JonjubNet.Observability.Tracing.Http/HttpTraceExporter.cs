using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using JonjubNet.Observability.Tracing.Core;
using JonjubNet.Observability.Tracing.Core.Interfaces;
using JonjubNet.Observability.Shared.Security;
using JonjubNet.Observability.Shared.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JonjubNet.Observability.Tracing.Http
{
    /// <summary>
    /// Exporter de traces para HTTP
    /// Exporta spans desde el Registry a un endpoint HTTP
    /// Similar a HttpLogSink pero para traces/spans
    /// </summary>
    public class HttpTraceExporter : ITraceSink
    {
        private readonly HttpOptions _options;
        private readonly ILogger<HttpTraceExporter>? _logger;
        private readonly HttpClient _httpClient;
        private readonly SecureHttpClientFactory? _httpClientFactory;
        private readonly EncryptionService? _encryptionService;
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptionsCache.GetDefault();

        public string Name => "Http";
        public bool IsEnabled => _options.Enabled;

        public HttpTraceExporter(
            IOptions<HttpOptions> options,
            ILogger<HttpTraceExporter>? logger = null,
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
                _logger?.LogError(ex, "Error exporting traces to HTTP endpoint");
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
                var payload = CreatePayload(spans);
                var content = new StringContent(payload, Encoding.UTF8, _options.DefaultContentType ?? "application/json");

                var response = await _httpClient.PostAsync(_options.EndpointUrl, content, cancellationToken);
                response.EnsureSuccessStatusCode();

                // Contar traces únicos sin LINQ (optimización)
                var uniqueTraces = new HashSet<string>();
                foreach (var span in spans)
                {
                    uniqueTraces.Add(span.TraceId);
                }
                _logger?.LogDebug("Sent {Count} spans ({TraceCount} traces) to HTTP endpoint {Endpoint}", 
                    spans.Count, uniqueTraces.Count, _options.EndpointUrl);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending traces batch to HTTP endpoint");
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
                    var payload = CreatePayload(batch);
                    var content = new StringContent(payload, Encoding.UTF8, _options.DefaultContentType ?? "application/json");

                    var response = await _httpClient.PostAsync(_options.EndpointUrl, content, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    // Contar traces únicos sin LINQ (optimización)
                    var uniqueTraces = new HashSet<string>();
                    foreach (var span in batch)
                    {
                        uniqueTraces.Add(span.TraceId);
                    }
                    _logger?.LogDebug("Sent batch {BatchNumber}/{TotalBatches} ({Count} spans, {TraceCount} traces) to HTTP endpoint {Endpoint}", 
                        i + 1, totalBatches, batch.Count, uniqueTraces.Count, _options.EndpointUrl);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error sending batch {BatchNumber} to HTTP endpoint", i + 1);
                    // Continuar con el siguiente batch en lugar de fallar todo
                }
            }
        }

        /// <summary>
        /// Crea el payload JSON a partir de los spans
        /// Agrupa spans por traceId para mantener la relación
        /// Optimizado: sin LINQ en hot path
        /// </summary>
        private string CreatePayload(IReadOnlyList<Span> spans)
        {
            // Agrupar spans por traceId sin LINQ (optimización)
            var tracesByTraceId = new Dictionary<string, List<Span>>();
            foreach (var span in spans)
            {
                if (!tracesByTraceId.TryGetValue(span.TraceId, out var spanList))
                {
                    spanList = new List<Span>();
                    tracesByTraceId[span.TraceId] = spanList;
                }
                spanList.Add(span);
            }

            // Construir payload sin LINQ (optimización)
            var traces = new List<object>(tracesByTraceId.Count);
            foreach (var traceGroup in tracesByTraceId)
            {
                var spanList = new List<object>(traceGroup.Value.Count);
                foreach (var span in traceGroup.Value)
                {
                    spanList.Add(new
                    {
                        spanId = span.SpanId,
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
                    });
                }
                traces.Add(new
                {
                    traceId = traceGroup.Key,
                    spanCount = traceGroup.Value.Count,
                    spans = spanList
                });
            }

            var payload = new
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                count = spans.Count,
                traceCount = tracesByTraceId.Count,
                traces = traces
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

