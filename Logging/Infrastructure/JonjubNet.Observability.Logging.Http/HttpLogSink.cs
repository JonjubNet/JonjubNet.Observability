using System.Text;
using System.Text.Json;
using JonjubNet.Observability.Logging.Core;
using JonjubNet.Observability.Logging.Core.Interfaces;
using JonjubNet.Observability.Shared.Security;
using JonjubNet.Observability.Shared.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JonjubNet.Observability.Logging.Http
{
    /// <summary>
    /// Sink de logs para HTTP
    /// Exporta logs desde el Registry a un endpoint HTTP
    /// Similar a InfluxSink de Metrics pero para logs
    /// </summary>
    public class HttpLogSink : ILogSink
    {
        private readonly HttpOptions _options;
        private readonly ILogger<HttpLogSink>? _logger;
        private readonly HttpClient _httpClient;
        private readonly SecureHttpClientFactory? _httpClientFactory;
        private readonly EncryptionService? _encryptionService;
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptionsCache.GetDefault();

        public string Name => "Http";
        public bool IsEnabled => _options.Enabled;

        public HttpLogSink(
            IOptions<HttpOptions> options,
            ILogger<HttpLogSink>? logger = null,
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
            
            // NOTA: Content-Type debe establecerse en HttpContent, no en DefaultRequestHeaders
            // Se establece correctamente en StringContent cuando se crea el contenido

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
        /// Exporta logs desde el Registry (método principal optimizado)
        /// </summary>
        public async ValueTask ExportFromRegistryAsync(LogRegistry registry, CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
                return;

            try
            {
                var logs = registry.GetAllLogsAndClear();
                
                if (logs.Count == 0)
                    return;

                // Si hay muchos logs, enviar en batches
                if (logs.Count > _options.BatchSize)
                {
                    await SendInBatchesAsync(logs, cancellationToken);
                }
                else
                {
                    await SendAllAsync(logs, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exporting logs to HTTP endpoint");
            }
        }

        /// <summary>
        /// Envía todos los logs en un solo batch
        /// </summary>
        private async Task SendAllAsync(IReadOnlyList<StructuredLogEntry> logs, CancellationToken cancellationToken)
        {
            if (logs.Count == 0)
                return;

            try
            {
                var payload = CreatePayload(logs);
                var content = new StringContent(payload, Encoding.UTF8, _options.DefaultContentType ?? "application/json");

                var response = await _httpClient.PostAsync(_options.EndpointUrl, content, cancellationToken);
                response.EnsureSuccessStatusCode();

                _logger?.LogDebug("Sent {Count} logs to HTTP endpoint {Endpoint}", logs.Count, _options.EndpointUrl);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending logs batch to HTTP endpoint");
                throw;
            }
        }

        /// <summary>
        /// Envía logs en múltiples batches
        /// </summary>
        private async Task SendInBatchesAsync(IReadOnlyList<StructuredLogEntry> logs, CancellationToken cancellationToken)
        {
            var batchSize = _options.BatchSize;
            var totalBatches = (int)Math.Ceiling((double)logs.Count / batchSize);

            for (int i = 0; i < totalBatches; i++)
            {
                var start = i * batchSize;
                var end = Math.Min(start + batchSize, logs.Count);
                var batch = logs.Skip(start).Take(end - start).ToList();

                try
                {
                    var payload = CreatePayload(batch);
                    var content = new StringContent(payload, Encoding.UTF8, _options.DefaultContentType ?? "application/json");

                    var response = await _httpClient.PostAsync(_options.EndpointUrl, content, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    _logger?.LogDebug("Sent batch {BatchNumber}/{TotalBatches} ({Count} logs) to HTTP endpoint {Endpoint}", 
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
        /// Crea el payload JSON a partir de los logs
        /// </summary>
        private string CreatePayload(IReadOnlyList<StructuredLogEntry> logs)
        {
            var payload = new
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                count = logs.Count,
                logs = logs.Select(log => new
                {
                    timestamp = log.Timestamp.ToUnixTimeMilliseconds(),
                    level = log.Level.ToString(),
                    category = log.Category.ToString(),
                    message = log.Message,
                    exception = log.Exception,
                    properties = log.Properties,
                    tags = log.Tags,
                    correlationId = log.CorrelationId,
                    requestId = log.RequestId,
                    sessionId = log.SessionId,
                    userId = log.UserId,
                    eventType = log.EventType ?? string.Empty,
                    operation = log.Operation,
                    durationMs = log.DurationMs
                }).ToList()
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
    }
}

