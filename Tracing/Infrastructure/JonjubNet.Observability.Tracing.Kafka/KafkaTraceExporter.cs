using JonjubNet.Observability.Tracing.Core;
using JonjubNet.Observability.Tracing.Core.Interfaces;
using JonjubNet.Observability.Shared.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JonjubNet.Observability.Tracing.Kafka
{
    /// <summary>
    /// Exporter de traces para Kafka
    /// Exporta spans desde el Registry a Kafka usando KafkaProducerFactory
    /// REUTILIZA código de Shared.Kafka (NO duplica)
    /// Similar a KafkaLogSink pero para traces/spans
    /// </summary>
    public class KafkaTraceExporter : ITraceSink
    {
        private readonly KafkaOptions _options;
        private readonly ILogger<KafkaTraceExporter>? _logger;
        private readonly IKafkaProducer _kafkaProducer;

        public string Name => "Kafka";
        public bool IsEnabled => _options.Enabled;

        public KafkaTraceExporter(
            IOptions<KafkaOptions> options,
            KafkaProducerFactory kafkaProducerFactory,
            ILogger<KafkaTraceExporter>? logger = null)
        {
            _options = options.Value;
            _logger = logger;

            // REUTILIZAR KafkaProducerFactory de Shared.Kafka
            _kafkaProducer = kafkaProducerFactory.CreateProducer(
                bootstrapServers: _options.BootstrapServers,
                producerUrl: _options.ProducerUrl,
                topic: _options.Topic,
                useWebhook: _options.UseWebhook,
                enabled: _options.Enabled,
                additionalConfig: _options.AdditionalConfig,
                headers: _options.Headers);
        }

        /// <summary>
        /// Exporta spans desde el Registry (método principal optimizado)
        /// </summary>
        public async ValueTask ExportFromRegistryAsync(TraceRegistry registry, CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
                return;

            try
            {
                var spans = registry.GetAllSpansAndClear();
                
                if (spans.Count == 0)
                    return;

                // Si hay muchos spans, enviar en batches
                if (spans.Count > _options.BatchSize)
                {
                    await SendInBatchesAsync(spans, cancellationToken);
                }
                else
                {
                    await SendAllAsync(spans, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exporting traces to Kafka");
            }
        }

        /// <summary>
        /// Envía todos los spans en un solo batch
        /// </summary>
        private async Task SendAllAsync(IReadOnlyList<Span> spans, CancellationToken cancellationToken)
        {
            if (spans.Count == 0)
                return;

            try
            {
                var batchMessage = KafkaTraceMessageFactory.CreateBatchMessage(spans);
                await _kafkaProducer.SendAsync(batchMessage, cancellationToken);
                _logger?.LogDebug("Sent {Count} spans ({TraceCount} traces) to Kafka topic {Topic}", 
                    spans.Count, spans.GroupBy(s => s.TraceId).Count(), _options.Topic);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending traces batch to Kafka");
                throw;
            }
        }

        /// <summary>
        /// Envía spans en múltiples batches
        /// </summary>
        private async Task SendInBatchesAsync(IReadOnlyList<Span> spans, CancellationToken cancellationToken)
        {
            var batchSize = _options.BatchSize;
            var totalBatches = (int)Math.Ceiling((double)spans.Count / batchSize);

            for (int i = 0; i < totalBatches; i++)
            {
                var start = i * batchSize;
                var end = Math.Min(start + batchSize, spans.Count);
                var batch = spans.Skip(start).Take(end - start).ToList();

                try
                {
                    var batchMessage = KafkaTraceMessageFactory.CreateBatchMessage(batch);
                    await _kafkaProducer.SendAsync(batchMessage, cancellationToken);
                    _logger?.LogDebug("Sent batch {BatchNumber}/{TotalBatches} ({Count} spans, {TraceCount} traces) to Kafka topic {Topic}", 
                        i + 1, totalBatches, batch.Count, batch.GroupBy(s => s.TraceId).Count(), _options.Topic);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error sending batch {BatchNumber} to Kafka", i + 1);
                    // Continuar con el siguiente batch en lugar de fallar todo
                }
            }
        }
    }
}

