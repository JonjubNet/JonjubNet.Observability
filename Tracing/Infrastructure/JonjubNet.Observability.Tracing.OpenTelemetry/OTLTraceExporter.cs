using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using JonjubNet.Observability.Tracing.Core;
using JonjubNet.Observability.Tracing.Core.Interfaces;
using JonjubNet.Observability.Shared.Security;
using JonjubNet.Observability.Shared.Utils;
using JonjubNet.Observability.Shared.OpenTelemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JonjubNet.Observability.Tracing.OpenTelemetry
{
    /// <summary>
    /// Exporter de traces para OpenTelemetry Collector
    /// Exporta spans en formato OTLP (OpenTelemetry Protocol)
    /// REUTILIZA código de Shared (Security, Utils) - NO duplica
    /// Similar a OTLLogSink pero para traces/spans
    /// </summary>
    public class OTLTraceExporter : ITraceSink
    {
        private readonly OTLTraceOptions _options;
        private readonly ILogger<OTLTraceExporter>? _logger;
        private readonly HttpClient? _httpClient;
        private readonly EncryptionService? _encryptionService;
        private readonly bool _encryptInTransit;
        
        // JsonSerializerOptions reutilizado (sin allocations)
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptionsCache.GetDefault();
        
        // String interning para SpanKind (optimización GC)
        private static readonly ConcurrentDictionary<SpanKind, string> _spanKindCache = new();
        
        // Cache estático de strings comunes para SpanKind (sin allocations)
        private static readonly string SpanKindInternal = string.Intern("SPAN_KIND_INTERNAL");
        private static readonly string SpanKindServer = string.Intern("SPAN_KIND_SERVER");
        private static readonly string SpanKindClient = string.Intern("SPAN_KIND_CLIENT");
        private static readonly string SpanKindProducer = string.Intern("SPAN_KIND_PRODUCER");
        private static readonly string SpanKindConsumer = string.Intern("SPAN_KIND_CONSUMER");

        public string Name => "OpenTelemetry";
        public bool IsEnabled => _options.Enabled;

        public OTLTraceExporter(
            IOptions<OTLTraceOptions> options,
            ILogger<OTLTraceExporter>? logger = null,
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
        /// Exporta spans desde el Registry (método principal optimizado)
        /// </summary>
        public async ValueTask ExportFromRegistryAsync(TraceRegistry registry, CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
                return;

            if (_httpClient == null)
            {
                _logger?.LogWarning("OpenTelemetry is enabled but HttpClient is not available. Skipping export.");
                return;
            }

            try
            {
                var spans = registry.GetAllSpansAndClear();

                if (spans.Count == 0)
                    return;

                // Enviar en batches si hay muchos spans (evita desbordamiento)
                if (spans.Count > _options.BatchSize)
                {
                    await SendInBatchesAsync(spans, cancellationToken);
                }
                else
                {
                    await SendAllAsync(spans, cancellationToken);
                }
            }
            catch (HttpRequestException httpEx) when (httpEx.InnerException is System.Net.Sockets.SocketException)
            {
                _logger?.LogWarning("OpenTelemetry endpoint not available: {Endpoint}. Traces will not be exported to OTLP. To disable, set Tracing:OpenTelemetry:Enabled to false.", _options.Endpoint);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exporting traces to OTLP endpoint {Endpoint}", _options.Endpoint);
            }
        }

        /// <summary>
        /// Envía todos los spans en un solo batch
        /// </summary>
        private async Task SendAllAsync(IReadOnlyList<Span> spans, CancellationToken cancellationToken)
        {
            if (spans.Count == 0 || _httpClient == null)
                return;

            try
            {
                var otlpPayload = ConvertSpansToOTLPFormat(spans);
                var url = OtlpUrlBuilder.BuildUrl(_options.Endpoint, _options.Protocol, "traces");
                
                // Usar helper compartido para crear HttpContent
                var content = OtlpContentBuilder.CreateContent(
                    otlpPayload,
                    _options.EnableCompression,
                    _encryptionService,
                    _encryptInTransit,
                    _logger);

                var response = await _httpClient.PostAsync(url, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger?.LogWarning("OTLP export failed with status {StatusCode}: {Error}",
                        response.StatusCode, errorContent);
                }
                else
                {
                    _logger?.LogDebug("Exported {Count} spans to OTLP endpoint {Endpoint}", spans.Count, _options.Endpoint);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending spans batch to OTLP endpoint");
                throw;
            }
        }

        /// <summary>
        /// Envía spans en múltiples batches (evita desbordamiento de memoria)
        /// </summary>
        private async Task SendInBatchesAsync(IReadOnlyList<Span> spans, CancellationToken cancellationToken)
        {
            if (_httpClient == null)
                return;

            var batchSize = _options.BatchSize;
            var totalBatches = (int)Math.Ceiling((double)spans.Count / batchSize);

            // Optimizado: crear array de tasks directamente (sin LINQ)
            var tasks = new Task[totalBatches];
            for (int i = 0; i < totalBatches; i++)
            {
                var start = i * batchSize;
                var end = Math.Min(start + batchSize, spans.Count);
                var batch = new List<Span>(end - start);
                
                // Copiar batch (evita mantener referencia a lista completa)
                for (int j = start; j < end; j++)
                {
                    batch.Add(spans[j]);
                }

                var batchNumber = i;
                tasks[i] = SendBatchAsync(batch, batchNumber + 1, totalBatches, cancellationToken);
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Envía un batch individual
        /// </summary>
        private async Task SendBatchAsync(IReadOnlyList<Span> batch, int batchNumber, int totalBatches, CancellationToken cancellationToken)
        {
            if (_httpClient == null || batch.Count == 0)
                return;

            try
            {
                var otlpPayload = ConvertSpansToOTLPFormat(batch);
                var url = OtlpUrlBuilder.BuildUrl(_options.Endpoint, _options.Protocol, "traces");
                
                // Usar helper compartido para crear HttpContent
                var content = OtlpContentBuilder.CreateContent(
                    otlpPayload,
                    _options.EnableCompression,
                    _encryptionService,
                    _encryptInTransit,
                    _logger);

                var response = await _httpClient.PostAsync(url, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger?.LogWarning("OTLP export failed for batch {BatchNumber}/{TotalBatches} with status {StatusCode}: {Error}",
                        batchNumber, totalBatches, response.StatusCode, errorContent);
                }
                else
                {
                    _logger?.LogDebug("Exported batch {BatchNumber}/{TotalBatches} ({Count} spans) to OTLP endpoint {Endpoint}",
                        batchNumber, totalBatches, batch.Count, _options.Endpoint);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending batch {BatchNumber} to OTLP endpoint", batchNumber);
                // No lanzar excepción - continuar con otros batches
            }
        }

        /// <summary>
        /// Convierte spans del Registry a formato OTLP
        /// Optimizado: evita allocations innecesarias, pre-allocate capacity
        /// </summary>
        private object ConvertSpansToOTLPFormat(IReadOnlyList<Span> spans)
        {
            if (spans.Count == 0)
                return new { resourceSpans = Array.Empty<object>() };

            // Agrupar spans por trace ID
            // Optimizado: pre-estimar capacidad basado en número de spans
            var tracesByTraceId = new Dictionary<string, List<Span>>(spans.Count / 2); // Estimación: ~2 spans por trace
            
            foreach (var span in spans)
            {
                // Optimizado: usar TryGetValue + Add en lugar de ContainsKey + indexer
                if (!tracesByTraceId.TryGetValue(span.TraceId, out var traceList))
                {
                    traceList = new List<Span>();
                    tracesByTraceId[span.TraceId] = traceList;
                }
                traceList.Add(span);
            }

            // Pre-allocate capacity para resourceSpans
            var resourceSpans = new List<object>(tracesByTraceId.Count);

            foreach (var traceGroup in tracesByTraceId)
            {
                var spansInTrace = traceGroup.Value;
                // Pre-allocate capacity para spanData
                var spanData = new List<object>(spansInTrace.Count);

                foreach (var span in spansInTrace)
                {
                    // Convertir timestamp a nanosegundos (formato OTLP)
                    var startTimeUnixNano = span.StartTime.ToUnixTimeMilliseconds() * 1_000_000;
                    var endTimeUnixNano = span.EndTime?.ToUnixTimeMilliseconds() * 1_000_000 ?? startTimeUnixNano;

                    // Convertir SpanId y TraceId de hex string a bytes (OTLP usa bytes)
                    var traceIdBytes = ConvertHexToBytes(span.TraceId);
                    var spanIdBytes = ConvertHexToBytes(span.SpanId);
                    var parentSpanIdBytes = !string.IsNullOrEmpty(span.ParentSpanId) 
                        ? ConvertHexToBytes(span.ParentSpanId) 
                        : null;

                    // Construir atributos (tags + properties)
                    // Optimizado: pre-estimar capacidad total de atributos
                    var estimatedAttributeCount = (span.Tags?.Count ?? 0) + (span.Properties?.Count ?? 0) + 3; // +3 para metadatos estándar
                    var attributes = new List<object>(estimatedAttributeCount);

                    // Agregar tags como atributos
                    if (span.Tags != null && span.Tags.Count > 0)
                    {
                        foreach (var tag in span.Tags)
                        {
                            attributes.Add(new
                            {
                                key = tag.Key,
                                value = new { stringValue = tag.Value }
                            });
                        }
                    }

                    // Agregar propiedades como atributos
                    if (span.Properties != null && span.Properties.Count > 0)
                    {
                        foreach (var prop in span.Properties)
                        {
                            var value = ConvertPropertyValue(prop.Value);
                            attributes.Add(new
                            {
                                key = prop.Key,
                                value = value
                            });
                        }
                    }

                    // Agregar metadatos estándar
                    if (!string.IsNullOrEmpty(span.ServiceName))
                    {
                        attributes.Add(new
                        {
                            key = "service.name",
                            value = new { stringValue = span.ServiceName }
                        });
                    }

                    if (!string.IsNullOrEmpty(span.ResourceName))
                    {
                        attributes.Add(new
                        {
                            key = "resource.name",
                            value = new { stringValue = span.ResourceName }
                        });
                    }

                    // Convertir eventos del span
                    // Optimizado: pre-allocate capacity
                    var events = new List<object>(span.Events?.Count ?? 0);
                    if (span.Events != null && span.Events.Count > 0)
                    {
                        foreach (var spanEvent in span.Events)
                        {
                            var eventTimeUnixNano = spanEvent.Timestamp.ToUnixTimeMilliseconds() * 1_000_000;
                            // Pre-allocate capacity para eventAttributes
                            var eventAttributes = new List<object>(spanEvent.Attributes?.Count ?? 0);
                            
                            if (spanEvent.Attributes != null && spanEvent.Attributes.Count > 0)
                            {
                                foreach (var attr in spanEvent.Attributes)
                                {
                                    var value = ConvertPropertyValue(attr.Value);
                                    eventAttributes.Add(new
                                    {
                                        key = attr.Key,
                                        value = value
                                    });
                                }
                            }

                            events.Add(new
                            {
                                timeUnixNano = eventTimeUnixNano,
                                name = spanEvent.Name,
                                attributes = eventAttributes.ToArray(),
                                droppedAttributesCount = 0
                            });
                        }
                    }

                    // Convertir status
                    var status = new
                    {
                        code = span.Status == SpanStatus.Ok ? "STATUS_CODE_OK" :
                               span.Status == SpanStatus.Error ? "STATUS_CODE_ERROR" : "STATUS_CODE_UNSET",
                        message = span.ErrorMessage
                    };

                    // Construir span OTLP
                    var spanDataObj = new
                    {
                        traceId = Convert.ToBase64String(traceIdBytes),
                        spanId = Convert.ToBase64String(spanIdBytes),
                        parentSpanId = parentSpanIdBytes != null ? Convert.ToBase64String(parentSpanIdBytes) : null,
                        name = span.OperationName,
                        kind = ConvertSpanKind(span.Kind),
                        startTimeUnixNano = startTimeUnixNano,
                        endTimeUnixNano = endTimeUnixNano,
                        attributes = attributes.ToArray(),
                        events = events.ToArray(),
                        status = status,
                        droppedAttributesCount = 0,
                        droppedEventsCount = 0,
                        droppedLinksCount = 0
                    };

                    spanData.Add(spanDataObj);
                }

                // Agregar resource span para este trace
                resourceSpans.Add(new
                {
                    resource = new { },
                    scopeSpans = new[]
                    {
                        new
                        {
                            scope = new { },
                            spans = spanData.ToArray()
                        }
                    }
                });
            }

            // Formato OTLP para traces
            var otlpPayload = new
            {
                resourceSpans = resourceSpans.ToArray()
            };

            return otlpPayload;
        }

        /// <summary>
        /// Convierte un valor de propiedad a formato OTLP
        /// </summary>
        private object ConvertPropertyValue(object? value)
        {
            if (value == null)
            {
                return new { stringValue = (string?)null };
            }

            return value switch
            {
                string str => new { stringValue = str },
                int i => new { intValue = i },
                long l => new { intValue = l },
                double d => new { doubleValue = d },
                float f => new { doubleValue = (double)f },
                bool b => new { boolValue = b },
                DateTime dt => new { stringValue = dt.ToString("O") },
                DateTimeOffset dto => new { stringValue = dto.ToString("O") },
                _ => new { stringValue = value.ToString() ?? string.Empty }
            };
        }

        /// <summary>
        /// Convierte SpanKind a formato OTLP
        /// Optimizado: usa strings internadas para evitar allocations
        /// </summary>
        private static string ConvertSpanKind(SpanKind kind)
        {
            return kind switch
            {
                SpanKind.Internal => SpanKindInternal,
                SpanKind.Server => SpanKindServer,
                SpanKind.Client => SpanKindClient,
                SpanKind.Producer => SpanKindProducer,
                SpanKind.Consumer => SpanKindConsumer,
                _ => SpanKindInternal
            };
        }

        /// <summary>
        /// Convierte un string hexadecimal a bytes
        /// </summary>
        private byte[] ConvertHexToBytes(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return Array.Empty<byte>();

            // OTLP espera 16 bytes para TraceId y 8 bytes para SpanId
            // Si el string es más largo, tomar los primeros bytes necesarios
            // Si es más corto, pad con ceros
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length && i * 2 < hex.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
    }
}
