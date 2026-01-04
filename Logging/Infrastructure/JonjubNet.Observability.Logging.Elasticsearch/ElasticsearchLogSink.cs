using System.Text;
using System.Text.Json;
using JonjubNet.Observability.Logging.Core;
using JonjubNet.Observability.Logging.Core.Interfaces;
using JonjubNet.Observability.Shared.Security;
using JonjubNet.Observability.Shared.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JonjubNet.Observability.Logging.Elasticsearch
{
    /// <summary>
    /// Sink de logs para Elasticsearch
    /// Exporta logs desde el Registry a Elasticsearch usando la API de _bulk
    /// Similar a InfluxSink de Metrics pero para logs
    /// </summary>
    public class ElasticsearchLogSink : ILogSink
    {
        private readonly ElasticsearchOptions _options;
        private readonly ILogger<ElasticsearchLogSink>? _logger;
        private readonly HttpClient _httpClient;
        private readonly SecureHttpClientFactory? _httpClientFactory;
        private readonly EncryptionService? _encryptionService;
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptionsCache.GetDefault();

        public string Name => "Elasticsearch";
        public bool IsEnabled => _options.Enabled;

        public ElasticsearchLogSink(
            IOptions<ElasticsearchOptions> options,
            ILogger<ElasticsearchLogSink>? logger = null,
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
            
            // NOTA: Content-Type debe establecerse en HttpContent, no en DefaultRequestHeaders
            // Se establece correctamente en StringContent cuando se crea el contenido

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

                // Elasticsearch _bulk API requiere formato NDJSON
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
                _logger?.LogError(ex, "Error exporting logs to Elasticsearch");
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
                var payload = CreateBulkPayload(logs);
                var content = new StringContent(payload, Encoding.UTF8, "application/x-ndjson");

                var url = $"{_options.BaseUrl}/{_options.IndexName}/_bulk";
                var response = await _httpClient.PostAsync(url, content, cancellationToken);
                response.EnsureSuccessStatusCode();

                _logger?.LogDebug("Sent {Count} logs to Elasticsearch index {Index}", logs.Count, _options.IndexName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending logs batch to Elasticsearch");
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
                    var payload = CreateBulkPayload(batch);
                    var content = new StringContent(payload, Encoding.UTF8, "application/x-ndjson");

                    var url = $"{_options.BaseUrl}/{_options.IndexName}/_bulk";
                    var response = await _httpClient.PostAsync(url, content, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    _logger?.LogDebug("Sent batch {BatchNumber}/{TotalBatches} ({Count} logs) to Elasticsearch index {Index}", 
                        i + 1, totalBatches, batch.Count, _options.IndexName);
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
        /// Formato: { "index": {} }\n{ log data }\n{ "index": {} }\n{ log data }\n...
        /// </summary>
        private string CreateBulkPayload(IReadOnlyList<StructuredLogEntry> logs)
        {
            var sb = new StringBuilder(logs.Count * 512); // Pre-allocate capacity

            foreach (var log in logs)
            {
                // Action line: { "index": { "_index": "logs", "_type": "_doc" } }
                var indexAction = new
                {
                    index = new
                    {
                        _index = _options.IndexName,
                        _type = _options.DocumentType
                    }
                };
                sb.AppendLine(JsonSerializer.Serialize(indexAction, JsonOptions));

                // Document line: el log como JSON
                var document = new
                {
                    timestamp = log.Timestamp,
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
                };
                sb.AppendLine(JsonSerializer.Serialize(document, JsonOptions));
            }

            return sb.ToString();
        }
    }
}

