using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;
using JonjubNet.Observability.Shared.Security;

namespace JonjubNet.Observability.Shared.Kafka
{
    /// <summary>
    /// Implementación de Kafka usando REST Proxy
    /// </summary>
    public class KafkaRestProxyProducer : IKafkaProducer
    {
        private readonly HttpClient _httpClient;
        private readonly string _topic;
        private readonly string _baseUrl;
        private readonly ILogger<KafkaRestProxyProducer>? _logger;
        private readonly bool _enabled;

        public bool IsEnabled => _enabled;

        public KafkaRestProxyProducer(
            string producerUrl,
            string topic,
            ILogger<KafkaRestProxyProducer>? logger = null,
            SecureHttpClientFactory? secureHttpClientFactory = null)
        {
            _logger = logger;
            _topic = topic;
            _baseUrl = producerUrl?.TrimEnd('/') ?? string.Empty;

            if (string.IsNullOrWhiteSpace(_baseUrl) || string.IsNullOrWhiteSpace(topic))
            {
                _enabled = false;
                _logger?.LogWarning("KafkaRestProxyProducer: ProducerUrl or Topic is empty, producer disabled");
                _httpClient = new HttpClient();
                return;
            }

            try
            {
                _httpClient = secureHttpClientFactory?.CreateSecureClient(_baseUrl) ?? new HttpClient();
                _httpClient.BaseAddress = new Uri(_baseUrl);
                // NOTA: Content-Type debe establecerse en HttpContent, no en DefaultRequestHeaders
                // Se establece correctamente en StringContent cuando se crea el contenido
                
                _enabled = true;
                _logger?.LogInformation("KafkaRestProxyProducer initialized for topic: {Topic} at {Url}", topic, _baseUrl);
            }
            catch (Exception ex)
            {
                _enabled = false;
                _logger?.LogError(ex, "Failed to initialize KafkaRestProxyProducer");
                _httpClient = new HttpClient();
            }
        }

        public async Task SendAsync(string message, CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
            {
                _logger?.LogWarning("KafkaRestProxyProducer: Cannot send message, producer is not enabled");
                return;
            }

            try
            {
                var payload = new
                {
                    records = new[]
                    {
                        new
                        {
                            value = message
                        }
                    }
                };

                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/vnd.kafka.json.v2+json");

                var url = $"/topics/{_topic}";
                var response = await _httpClient.PostAsync(url, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogDebug("KafkaRestProxyProducer: Message sent to topic {Topic}", _topic);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger?.LogError("KafkaRestProxyProducer: Failed to send message. Status: {Status}, Error: {Error}",
                        response.StatusCode, errorContent);
                    throw new HttpRequestException($"Failed to send message to Kafka REST Proxy: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "KafkaRestProxyProducer: Error sending message to topic {Topic}", _topic);
                throw;
            }
        }

        public async Task SendBatchAsync(IEnumerable<string> messages, CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
            {
                _logger?.LogWarning("KafkaRestProxyProducer: Cannot send batch, producer is not enabled");
                return;
            }

            try
            {
                var records = messages.Select(msg => new { value = msg }).ToArray();
                var payload = new { records };
                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/vnd.kafka.json.v2+json");

                var url = $"/topics/{_topic}";
                var response = await _httpClient.PostAsync(url, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogDebug("KafkaRestProxyProducer: Batch of {Count} messages sent to topic {Topic}", records.Length, _topic);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger?.LogError("KafkaRestProxyProducer: Failed to send batch. Status: {Status}, Error: {Error}",
                        response.StatusCode, errorContent);
                    throw new HttpRequestException($"Failed to send batch to Kafka REST Proxy: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "KafkaRestProxyProducer: Error sending batch to topic {Topic}", _topic);
                throw;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}

