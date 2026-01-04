using System.Text;
using JonjubNet.Observability.Logging.Core;
using JonjubNet.Observability.Logging.Core.Interfaces;
using JonjubNet.Observability.Shared.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JonjubNet.Observability.Logging.Kafka
{
    /// <summary>
    /// Sink de logs para Kafka
    /// Exporta logs desde el Registry a Kafka usando KafkaProducerFactory
    /// REUTILIZA código de Shared.Kafka (NO duplica)
    /// </summary>
    public class KafkaLogSink : ILogSink
    {
        private readonly KafkaOptions _options;
        private readonly ILogger<KafkaLogSink>? _logger;
        private readonly IKafkaProducer _kafkaProducer;

        public string Name => "Kafka";
        public bool IsEnabled => _options.Enabled;

        public KafkaLogSink(
            IOptions<KafkaOptions> options,
            KafkaProducerFactory kafkaProducerFactory,
            ILogger<KafkaLogSink>? logger = null)
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
                _logger?.LogError(ex, "Error exporting logs to Kafka");
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
                var batchMessage = KafkaLogMessageFactory.CreateBatchMessage(logs);
                await _kafkaProducer.SendAsync(batchMessage, cancellationToken);
                _logger?.LogDebug("Sent {Count} logs to Kafka topic {Topic}", logs.Count, _options.Topic);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending logs batch to Kafka");
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
                    var batchMessage = KafkaLogMessageFactory.CreateBatchMessage(batch);
                    await _kafkaProducer.SendAsync(batchMessage, cancellationToken);
                    _logger?.LogDebug("Sent batch {BatchNumber}/{TotalBatches} ({Count} logs) to Kafka topic {Topic}", 
                        i + 1, totalBatches, batch.Count, _options.Topic);
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

