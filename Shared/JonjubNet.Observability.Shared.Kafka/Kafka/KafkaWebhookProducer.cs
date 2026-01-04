using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;
using JonjubNet.Observability.Shared.Security;

namespace JonjubNet.Observability.Shared.Kafka
{
    /// <summary>
    /// Implementación de Kafka usando Webhook HTTP
    /// </summary>
    public class KafkaWebhookProducer : IKafkaProducer
    {
        private readonly HttpClient _httpClient;
        private readonly string _webhookUrl;
        private readonly ILogger<KafkaWebhookProducer>? _logger;
        private readonly bool _enabled;

        public bool IsEnabled => _enabled;

        public KafkaWebhookProducer(
            string webhookUrl,
            ILogger<KafkaWebhookProducer>? logger = null,
            SecureHttpClientFactory? secureHttpClientFactory = null,
            Dictionary<string, string>? headers = null)
        {
            _logger = logger;
            _webhookUrl = webhookUrl?.TrimEnd('/') ?? string.Empty;

            if (string.IsNullOrWhiteSpace(_webhookUrl))
            {
                _enabled = false;
                _logger?.LogWarning("KafkaWebhookProducer: WebhookUrl is empty, producer disabled");
                _httpClient = new HttpClient();
                return;
            }

            try
            {
                _httpClient = secureHttpClientFactory?.CreateSecureClient(_webhookUrl) ?? new HttpClient();
                _httpClient.BaseAddress = new Uri(_webhookUrl);
                // NOTA: Content-Type debe establecerse en HttpContent, no en DefaultRequestHeaders
                // Se establece correctamente en StringContent cuando se crea el contenido

                // Agregar headers personalizados si se proporcionan
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }

                _enabled = true;
                _logger?.LogInformation("KafkaWebhookProducer initialized for URL: {Url}", _webhookUrl);
            }
            catch (Exception ex)
            {
                _enabled = false;
                _logger?.LogError(ex, "Failed to initialize KafkaWebhookProducer");
                _httpClient = new HttpClient();
            }
        }

        public async Task SendAsync(string message, CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
            {
                _logger?.LogWarning("KafkaWebhookProducer: Cannot send message, producer is not enabled");
                return;
            }

            try
            {
                var content = new StringContent(message, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(string.Empty, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogDebug("KafkaWebhookProducer: Message sent to webhook {Url}", _webhookUrl);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger?.LogError("KafkaWebhookProducer: Failed to send message. Status: {Status}, Error: {Error}",
                        response.StatusCode, errorContent);
                    throw new HttpRequestException($"Failed to send message to webhook: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "KafkaWebhookProducer: Error sending message to webhook {Url}", _webhookUrl);
                throw;
            }
        }

        public async Task SendBatchAsync(IEnumerable<string> messages, CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
            {
                _logger?.LogWarning("KafkaWebhookProducer: Cannot send batch, producer is not enabled");
                return;
            }

            // Para webhook, enviar como array JSON
            try
            {
                var messagesArray = messages.ToArray();
                var batchJson = System.Text.Json.JsonSerializer.Serialize(messagesArray);
                var content = new StringContent(batchJson, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(string.Empty, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogDebug("KafkaWebhookProducer: Batch of {Count} messages sent to webhook {Url}", messagesArray.Length, _webhookUrl);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger?.LogError("KafkaWebhookProducer: Failed to send batch. Status: {Status}, Error: {Error}",
                        response.StatusCode, errorContent);
                    throw new HttpRequestException($"Failed to send batch to webhook: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "KafkaWebhookProducer: Error sending batch to webhook {Url}", _webhookUrl);
                throw;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}

