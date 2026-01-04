using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.IO.Compression;
using JonjubNet.Observability.Logging.Core;
using JonjubNet.Observability.Logging.Core.Interfaces;
using JonjubNet.Observability.Shared.Security;
using JonjubNet.Observability.Shared.Utils;
using JonjubNet.Observability.Shared.OpenTelemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JonjubNet.Observability.Logging.OpenTelemetry
{
    /// <summary>
    /// Sink de logs para OpenTelemetry Collector
    /// Exporta logs en formato OTLP (OpenTelemetry Protocol)
    /// REUTILIZA código de Shared (Security, Utils) - NO duplica
    /// Similar a OTLPExporter de Metrics pero para logs
    /// </summary>
    public class OTLLogSink : ILogSink
    {
        private readonly OTLLogOptions _options;
        private readonly ILogger<OTLLogSink>? _logger;
        private readonly HttpClient? _httpClient;
        private readonly EncryptionService? _encryptionService;
        private readonly bool _encryptInTransit;
        
        // String interning para niveles de log (optimización GC)
        private static readonly ConcurrentDictionary<string, string> _levelInternCache = new();
        
        // JsonSerializerOptions reutilizado (sin allocations)
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptionsCache.GetDefault();

        public string Name => "OpenTelemetry";
        public bool IsEnabled => _options.Enabled;

        public OTLLogSink(
            IOptions<OTLLogOptions> options,
            ILogger<OTLLogSink>? logger = null,
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
        /// Exporta logs desde el Registry (método principal optimizado)
        /// </summary>
        public async ValueTask ExportFromRegistryAsync(LogRegistry registry, CancellationToken cancellationToken = default)
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
                var logs = registry.GetAllLogsAndClear();

                if (logs.Count == 0)
                    return;

                // Enviar en batches si hay muchos logs (evita desbordamiento)
                if (logs.Count > _options.BatchSize)
                {
                    await SendInBatchesAsync(logs, cancellationToken);
                }
                else
                {
                    await SendAllAsync(logs, cancellationToken);
                }
            }
            catch (HttpRequestException httpEx) when (httpEx.InnerException is System.Net.Sockets.SocketException)
            {
                _logger?.LogWarning("OpenTelemetry endpoint not available: {Endpoint}. Logs will not be exported to OTLP. To disable, set Logging:OpenTelemetry:Enabled to false.", _options.Endpoint);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exporting logs to OTLP endpoint {Endpoint}", _options.Endpoint);
            }
        }

        /// <summary>
        /// Envía todos los logs en un solo batch
        /// </summary>
        private async Task SendAllAsync(IReadOnlyList<StructuredLogEntry> logs, CancellationToken cancellationToken)
        {
            if (logs.Count == 0 || _httpClient == null)
                return;

            try
            {
                var otlpPayload = ConvertLogsToOTLPFormat(logs);
                var url = OtlpUrlBuilder.BuildUrl(_options.Endpoint, _options.Protocol, "logs");
                
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
                    _logger?.LogDebug("Exported {Count} logs to OTLP endpoint {Endpoint}", logs.Count, _options.Endpoint);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending logs batch to OTLP endpoint");
                throw;
            }
        }

        /// <summary>
        /// Envía logs en múltiples batches (evita desbordamiento de memoria)
        /// </summary>
        private async Task SendInBatchesAsync(IReadOnlyList<StructuredLogEntry> logs, CancellationToken cancellationToken)
        {
            if (_httpClient == null)
                return;

            var batchSize = _options.BatchSize;
            var totalBatches = (int)Math.Ceiling((double)logs.Count / batchSize);

            // Optimizado: crear array de tasks directamente (sin LINQ)
            var tasks = new Task[totalBatches];
            for (int i = 0; i < totalBatches; i++)
            {
                var start = i * batchSize;
                var end = Math.Min(start + batchSize, logs.Count);
                var batch = new List<StructuredLogEntry>(end - start);
                
                // Copiar batch (evita mantener referencia a lista completa)
                for (int j = start; j < end; j++)
                {
                    batch.Add(logs[j]);
                }

                var batchNumber = i;
                tasks[i] = SendBatchAsync(batch, batchNumber + 1, totalBatches, cancellationToken);
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Envía un batch individual
        /// </summary>
        private async Task SendBatchAsync(IReadOnlyList<StructuredLogEntry> batch, int batchNumber, int totalBatches, CancellationToken cancellationToken)
        {
            if (_httpClient == null || batch.Count == 0)
                return;

            try
            {
                var otlpPayload = ConvertLogsToOTLPFormat(batch);
                var url = OtlpUrlBuilder.BuildUrl(_options.Endpoint, _options.Protocol, "logs");
                
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
                    _logger?.LogDebug("Exported batch {BatchNumber}/{TotalBatches} ({Count} logs) to OTLP endpoint {Endpoint}",
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
        /// Convierte logs del Registry a formato OTLP
        /// Optimizado: evita allocations innecesarias, usa string interning
        /// </summary>
        private object ConvertLogsToOTLPFormat(IReadOnlyList<StructuredLogEntry> logs)
        {
            var logRecords = new List<object>(logs.Count); // Pre-allocate capacity

            foreach (var log in logs)
            {
                // String interning para nivel de log (optimización GC)
                var severityText = InternLogLevel(log.Level.ToString());

                // Convertir timestamp a nanosegundos (formato OTLP)
                var timeUnixNano = log.Timestamp.ToUnixTimeMilliseconds() * 1_000_000;

                // Construir atributos (tags + properties + metadata)
                var attributes = new List<object>();

                // Agregar tags como atributos
                if (log.Tags != null && log.Tags.Count > 0)
                {
                    foreach (var tag in log.Tags)
                    {
                        attributes.Add(new
                        {
                            key = tag.Key,
                            value = new { stringValue = tag.Value }
                        });
                    }
                }

                // Agregar propiedades como atributos
                if (log.Properties != null && log.Properties.Count > 0)
                {
                    foreach (var prop in log.Properties)
                    {
                        // Convertir valor a formato OTLP según tipo
                        var value = ConvertPropertyValue(prop.Value);
                        attributes.Add(new
                        {
                            key = prop.Key,
                            value = value
                        });
                    }
                }

                // Agregar metadatos estándar
                if (!string.IsNullOrEmpty(log.Category))
                {
                    attributes.Add(new
                    {
                        key = "log.category",
                        value = new { stringValue = log.Category }
                    });
                }

                if (!string.IsNullOrEmpty(log.CorrelationId))
                {
                    attributes.Add(new
                    {
                        key = "trace.trace_id", // Estándar OpenTelemetry
                        value = new { stringValue = log.CorrelationId }
                    });
                }

                if (!string.IsNullOrEmpty(log.RequestId))
                {
                    attributes.Add(new
                    {
                        key = "request.id",
                        value = new { stringValue = log.RequestId }
                    });
                }

                if (!string.IsNullOrEmpty(log.SessionId))
                {
                    attributes.Add(new
                    {
                        key = "session.id",
                        value = new { stringValue = log.SessionId }
                    });
                }

                if (!string.IsNullOrEmpty(log.UserId))
                {
                    attributes.Add(new
                    {
                        key = "user.id",
                        value = new { stringValue = log.UserId }
                    });
                }

                if (!string.IsNullOrEmpty(log.EventType))
                {
                    attributes.Add(new
                    {
                        key = "event.type",
                        value = new { stringValue = log.EventType }
                    });
                }

                if (!string.IsNullOrEmpty(log.Operation))
                {
                    attributes.Add(new
                    {
                        key = "operation.name",
                        value = new { stringValue = log.Operation }
                    });
                }

                if (log.DurationMs.HasValue)
                {
                    attributes.Add(new
                    {
                        key = "operation.duration_ms",
                        value = new { intValue = log.DurationMs.Value }
                    });
                }

                // Construir log record OTLP
                object logRecord;
                
                // Agregar información de excepción si existe
                if (log.Exception != null)
                {
                    var exceptionAttributes = new List<object>
                    {
                        new { key = "exception.type", value = new { stringValue = log.Exception.GetType().Name } },
                        new { key = "exception.message", value = new { stringValue = log.Exception.Message } }
                    };

                    if (!string.IsNullOrEmpty(log.Exception.StackTrace))
                    {
                        exceptionAttributes.Add(new
                        {
                            key = "exception.stacktrace",
                            value = new { stringValue = log.Exception.StackTrace }
                        });
                    }

                    // Agregar atributos de excepción al log record
                    var allAttributes = new List<object>(attributes);
                    allAttributes.AddRange(exceptionAttributes);
                    
                    logRecord = new
                    {
                        timeUnixNano = timeUnixNano,
                        severityNumber = (int)log.Level + 1, // OTLP: 1=TRACE, 2=DEBUG, ..., 6=CRITICAL
                        severityText = severityText,
                        body = new { stringValue = log.Message },
                        attributes = allAttributes.ToArray(),
                        droppedAttributesCount = 0
                    };
                }
                else
                {
                    logRecord = new
                    {
                        timeUnixNano = timeUnixNano,
                        severityNumber = (int)log.Level + 1,
                        severityText = severityText,
                        body = new { stringValue = log.Message },
                        attributes = attributes.ToArray(),
                        droppedAttributesCount = 0
                    };
                }

                logRecords.Add(logRecord);
            }

            // Formato OTLP para logs
            var resourceLogs = new
            {
                resourceLogs = new[]
                {
                    new
                    {
                        resource = new { },
                        scopeLogs = new[]
                        {
                            new
                            {
                                scope = new { },
                                logRecords = logRecords.ToArray()
                            }
                        }
                    }
                }
            };

            return resourceLogs;
        }

        /// <summary>
        /// Convierte un valor de propiedad a formato OTLP
        /// Optimizado: evita boxings innecesarios
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
        /// String interning para niveles de log (optimización GC)
        /// </summary>
        private static string InternLogLevel(string level)
        {
            return _levelInternCache.GetOrAdd(level, level);
        }
    }
}

