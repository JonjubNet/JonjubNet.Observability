using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Shared.Kafka
{
    /// <summary>
    /// Implementación nula de IKafkaProducer para cuando Kafka está deshabilitado
    /// </summary>
    public class NullKafkaProducer : IKafkaProducer
    {
        private readonly ILogger<NullKafkaProducer>? _logger;

        public bool IsEnabled => false;

        public NullKafkaProducer(ILogger<NullKafkaProducer>? logger = null)
        {
            _logger = logger;
        }

        public Task SendAsync(string message, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("NullKafkaProducer: Message would be sent (Kafka disabled)");
            return Task.CompletedTask;
        }

        public Task SendBatchAsync(IEnumerable<string> messages, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("NullKafkaProducer: {Count} messages would be sent (Kafka disabled)", messages?.Count() ?? 0);
            return Task.CompletedTask;
        }
    }
}

