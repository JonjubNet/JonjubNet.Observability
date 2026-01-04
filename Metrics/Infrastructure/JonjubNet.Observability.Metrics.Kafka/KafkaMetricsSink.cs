using System.Text;
using System.Text.Json;
using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.Core.Interfaces;
using JonjubNet.Observability.Metrics.Core.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JonjubNet.Observability.Metrics.Kafka
{
    /// <summary>
    /// Sink de métricas para Kafka
    /// </summary>
    public class KafkaMetricsSink : IMetricsSink
    {
        private readonly KafkaOptions _options;
        private readonly ILogger<KafkaMetricsSink>? _logger;

        public string Name => "Kafka";
        public bool IsEnabled => _options.Enabled;

        public KafkaMetricsSink(
            IOptions<KafkaOptions> options,
            ILogger<KafkaMetricsSink>? logger = null)
        {
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Exporta métricas desde el Registry (método principal optimizado)
        /// </summary>
        public async ValueTask ExportFromRegistryAsync(MetricRegistry registry, CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
                return;

            try
            {
                var messages = CollectionPool.RentStringList();
                try
                {
                    // Convertir Registry a mensajes Kafka
                    ConvertRegistryToKafkaMessages(registry, messages);

                    if (messages.Count > 0)
                    {
                        // TODO: Replace with actual Kafka producer
                        // Example with Confluent.Kafka:
                        // using var producer = new ProducerBuilder<string, string>(config).Build();
                        // foreach (var message in messages)
                        // {
                        //     await producer.ProduceAsync(_options.Topic, new Message<string, string> { Value = message }, cancellationToken);
                        // }

                        _logger?.LogDebug("Kafka (logging fallback): Would send {Count} messages to topic {Topic}", 
                            messages.Count, _options.Topic);
                        
                        if (messages.Count > 0)
                        {
                            _logger?.LogTrace("Sample Kafka message: {Message}", messages[0]);
                        }
                    }
                }
                finally
                {
                    CollectionPool.ReturnStringList(messages);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exporting metrics to Kafka");
            }

            await Task.CompletedTask;
        }

        private void ConvertRegistryToKafkaMessages(MetricRegistry registry, List<string> messages)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Convertir Counters
            foreach (var counter in registry.GetAllCounters().Values)
            {
                foreach (var (key, value) in counter.GetAllValues())
                {
                    var tags = ParseKey(key);
                    var message = KafkaMessageFactory.CreateFromRegistry(
                        counter.Name,
                        counter.Description,
                        "counter",
                        value,
                        tags,
                        timestamp
                    );
                    messages.Add(message);
                }
            }

            // Convertir Gauges
            foreach (var gauge in registry.GetAllGauges().Values)
            {
                foreach (var (key, value) in gauge.GetAllValues())
                {
                    var tags = ParseKey(key);
                    var message = KafkaMessageFactory.CreateFromRegistry(
                        gauge.Name,
                        gauge.Description,
                        "gauge",
                        value,
                        tags,
                        timestamp
                    );
                    messages.Add(message);
                }
            }

            // Similar para Histograms y Summaries...
        }

        private Dictionary<string, string> ParseKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return new Dictionary<string, string>();

            var result = new Dictionary<string, string>();
            var pairs = key.Split(',');
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=');
                if (parts.Length == 2)
                {
                    result[parts[0]] = parts[1];
                }
            }
            return result;
        }

    }
}
