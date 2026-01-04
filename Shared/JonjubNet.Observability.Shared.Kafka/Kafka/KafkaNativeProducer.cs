using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using JonjubNet.Observability.Shared.Context;

namespace JonjubNet.Observability.Shared.Kafka
{
    /// <summary>
    /// Implementación nativa de Kafka usando Confluent.Kafka
    /// Soporta propagación automática de CorrelationId en headers
    /// Thread-safe, sin overhead innecesario, optimizado para performance
    /// </summary>
    public class KafkaNativeProducer : IKafkaProducerWithHeaders
    {
        private readonly IProducer<string, string>? _producer;
        private readonly string _topic;
        private readonly ILogger<KafkaNativeProducer>? _logger;
        private readonly bool _enabled;

        public bool IsEnabled => _enabled && _producer != null;

        public KafkaNativeProducer(
            string bootstrapServers,
            string topic,
            ILogger<KafkaNativeProducer>? logger = null,
            Dictionary<string, string>? additionalConfig = null)
        {
            _logger = logger;
            _topic = topic;

            if (string.IsNullOrWhiteSpace(bootstrapServers) || string.IsNullOrWhiteSpace(topic))
            {
                _enabled = false;
                _logger?.LogWarning("KafkaNativeProducer: BootstrapServers or Topic is empty, producer disabled");
                return;
            }

            try
            {
                var config = new ProducerConfig
                {
                    BootstrapServers = bootstrapServers,
                    Acks = Acks.All,
                    EnableIdempotence = true,
                    MessageSendMaxRetries = 3
                };

                // Agregar configuración adicional si se proporciona
                if (additionalConfig != null)
                {
                    foreach (var kvp in additionalConfig)
                    {
                        config.Set(kvp.Key, kvp.Value);
                    }
                }

                _producer = new ProducerBuilder<string, string>(config)
                    .SetErrorHandler((producer, error) =>
                    {
                        _logger?.LogError("Kafka producer error: {Error}", error);
                    })
                    .Build();

                _enabled = true;
                _logger?.LogInformation("KafkaNativeProducer initialized for topic: {Topic}", topic);
            }
            catch (Exception ex)
            {
                _enabled = false;
                _logger?.LogError(ex, "Failed to initialize KafkaNativeProducer");
            }
        }

        public async Task SendAsync(string message, CancellationToken cancellationToken = default)
        {
            // Optimización: usar helper centralizado para evitar duplicación
            var headers = CorrelationPropagationHelper.CreateHeadersWithCorrelationId();
            await SendAsync(message, headers, cancellationToken);
        }

        public async Task SendAsync(string message, Dictionary<string, string>? headers, CancellationToken cancellationToken = default)
        {
            if (!IsEnabled || _producer == null)
            {
                _logger?.LogWarning("KafkaNativeProducer: Cannot send message, producer is not enabled");
                return;
            }

            try
            {
                var kafkaMessage = new Message<string, string>
                {
                    Value = message
                };

                // Agregar headers si se proporcionan (optimización: usar helper centralizado)
                if (headers != null && headers.Count > 0)
                {
                    kafkaMessage.Headers = new Headers();
                    foreach (var header in headers)
                    {
                        kafkaMessage.Headers.Add(header.Key, System.Text.Encoding.UTF8.GetBytes(header.Value));
                    }
                }
                else
                {
                    // Si no hay headers proporcionados, intentar agregar CorrelationId del contexto
                    // Optimización: usar helper centralizado para evitar duplicación
                    var kafkaHeaders = KafkaCorrelationHelper.CreateKafkaHeaders();
                    if (kafkaHeaders != null)
                    {
                        kafkaMessage.Headers = kafkaHeaders;
                    }
                }

                var deliveryResult = await _producer.ProduceAsync(_topic, kafkaMessage, cancellationToken);
                _logger?.LogDebug("KafkaNativeProducer: Message sent to topic {Topic}, partition {Partition}, offset {Offset}",
                    deliveryResult.Topic, deliveryResult.Partition, deliveryResult.Offset);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "KafkaNativeProducer: Error sending message to topic {Topic}", _topic);
                throw;
            }
        }

        public async Task SendBatchAsync(IEnumerable<string> messages, CancellationToken cancellationToken = default)
        {
            // Optimización: usar helper centralizado para evitar duplicación
            var headers = CorrelationPropagationHelper.CreateHeadersWithCorrelationId();
            await SendBatchAsync(messages, headers, cancellationToken);
        }

        public async Task SendBatchAsync(IEnumerable<string> messages, Dictionary<string, string>? headers, CancellationToken cancellationToken = default)
        {
            if (!IsEnabled || _producer == null)
            {
                _logger?.LogWarning("KafkaNativeProducer: Cannot send batch, producer is not enabled");
                return;
            }

            // Optimización: pre-allocar capacidad para tasks si es posible
            var messagesList = messages as IList<string> ?? messages.ToList();
            var tasks = new List<Task>(messagesList.Count);
            
            foreach (var msg in messagesList)
            {
                tasks.Add(SendAsync(msg, headers, cancellationToken));
            }
            
            await Task.WhenAll(tasks);
        }

        public void Dispose()
        {
            _producer?.Dispose();
        }
    }
}

