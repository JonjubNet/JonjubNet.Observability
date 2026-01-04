using Microsoft.Extensions.Logging;
using JonjubNet.Observability.Shared.Security;

namespace JonjubNet.Observability.Shared.Kafka
{
    /// <summary>
    /// Factory para crear instancias de IKafkaProducer según la configuración
    /// Común para Metrics y Logging
    /// </summary>
    public class KafkaProducerFactory
    {
        private readonly ILogger? _logger;
        private readonly SecureHttpClientFactory? _secureHttpClientFactory;

        public KafkaProducerFactory(
            ILogger? logger = null,
            SecureHttpClientFactory? secureHttpClientFactory = null)
        {
            _logger = logger;
            _secureHttpClientFactory = secureHttpClientFactory;
        }

        /// <summary>
        /// Crea un producer de Kafka según la configuración
        /// </summary>
        /// <param name="bootstrapServers">Bootstrap servers para conexión nativa (tiene prioridad)</param>
        /// <param name="producerUrl">URL para REST Proxy o Webhook</param>
        /// <param name="topic">Topic de Kafka</param>
        /// <param name="useWebhook">Si es true, usa Webhook; si es false, usa REST Proxy</param>
        /// <param name="enabled">Si está habilitado</param>
        /// <param name="additionalConfig">Configuración adicional para producer nativo</param>
        /// <param name="headers">Headers adicionales para webhook</param>
        /// <returns>Instancia de IKafkaProducer</returns>
        public IKafkaProducer CreateProducer(
            string? bootstrapServers = null,
            string? producerUrl = null,
            string? topic = null,
            bool useWebhook = false,
            bool enabled = true,
            Dictionary<string, string>? additionalConfig = null,
            Dictionary<string, string>? headers = null)
        {
            if (!enabled)
            {
                _logger?.LogDebug("KafkaProducerFactory: Creating NullKafkaProducer (disabled)");
                return new NullKafkaProducer(_logger as ILogger<NullKafkaProducer>);
            }

            // Prioridad 1: Conexión nativa si BootstrapServers está configurado
            if (!string.IsNullOrWhiteSpace(bootstrapServers) && !string.IsNullOrWhiteSpace(topic))
            {
                _logger?.LogDebug("KafkaProducerFactory: Creating KafkaNativeProducer");
                return new KafkaNativeProducer(
                    bootstrapServers,
                    topic,
                    _logger as ILogger<KafkaNativeProducer>,
                    additionalConfig);
            }

            // Prioridad 2: REST Proxy si ProducerUrl está configurado y no es webhook
            if (!string.IsNullOrWhiteSpace(producerUrl) && !string.IsNullOrWhiteSpace(topic) && !useWebhook)
            {
                _logger?.LogDebug("KafkaProducerFactory: Creating KafkaRestProxyProducer");
                return new KafkaRestProxyProducer(
                    producerUrl,
                    topic,
                    _logger as ILogger<KafkaRestProxyProducer>,
                    _secureHttpClientFactory);
            }

            // Prioridad 3: Webhook si ProducerUrl está configurado y useWebhook es true
            if (!string.IsNullOrWhiteSpace(producerUrl) && useWebhook)
            {
                _logger?.LogDebug("KafkaProducerFactory: Creating KafkaWebhookProducer");
                return new KafkaWebhookProducer(
                    producerUrl,
                    _logger as ILogger<KafkaWebhookProducer>,
                    _secureHttpClientFactory,
                    headers);
            }

            // Fallback: Null producer si no hay configuración válida
            _logger?.LogWarning("KafkaProducerFactory: No valid configuration found, creating NullKafkaProducer");
            return new NullKafkaProducer(_logger as ILogger<NullKafkaProducer>);
        }
    }
}

