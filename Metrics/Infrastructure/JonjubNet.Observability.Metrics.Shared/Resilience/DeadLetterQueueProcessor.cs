using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.Core.Interfaces;
using JonjubNet.Observability.Metrics.Core.Resilience;
using JonjubNet.Observability.Metrics.Core.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Metrics.Shared.Resilience
{
    /// <summary>
    /// Procesador de Dead Letter Queue que reintenta periódicamente las métricas fallidas
    /// </summary>
    public class DeadLetterQueueProcessor : BackgroundService
    {
        private readonly DeadLetterQueue _deadLetterQueue;
        private readonly IEnumerable<IMetricsSink> _sinks;
        private readonly RetryPolicy? _retryPolicy;
        private readonly ILogger<DeadLetterQueueProcessor>? _logger;
        private readonly TimeSpan _processInterval;
        private readonly int _batchSize;

        public DeadLetterQueueProcessor(
            DeadLetterQueue deadLetterQueue,
            IEnumerable<IMetricsSink> sinks,
            RetryPolicy? retryPolicy = null,
            TimeSpan? processInterval = null,
            int batchSize = 100,
            ILogger<DeadLetterQueueProcessor>? logger = null)
        {
            _deadLetterQueue = deadLetterQueue;
            _sinks = sinks;
            _retryPolicy = retryPolicy;
            _logger = logger;
            _processInterval = processInterval ?? TimeSpan.FromMinutes(5);
            _batchSize = batchSize;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger?.LogInformation("Dead Letter Queue Processor started. Processing interval: {Interval}", _processInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessFailedMetricsAsync(stoppingToken);
                    await Task.Delay(_processInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger?.LogInformation("Dead Letter Queue Processor stopped");
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in Dead Letter Queue Processor");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Esperar antes de reintentar
                }
            }
        }

        /// <summary>
        /// Procesa métricas fallidas de la DLQ
        /// </summary>
        private async Task ProcessFailedMetricsAsync(CancellationToken cancellationToken)
        {
            var stats = _deadLetterQueue.GetStats();
            if (stats.Count == 0)
            {
                return; // No hay métricas para procesar
            }

            _logger?.LogInformation("Processing {Count} failed metrics from DLQ", stats.Count);

            var processedCount = 0;
            var successCount = 0;
            var failedCount = 0;

            // Procesar en batches
            while (_deadLetterQueue.TryDequeue(out var failedMetric) && processedCount < _batchSize)
            {
                try
                {
                    if (failedMetric == null)
                    {
                        continue;
                    }

                    var sinkName = failedMetric.SinkName;
                    var sink = _sinks.FirstOrDefault(s => s.Name == sinkName && s.IsEnabled);
                    if (sink == null)
                    {
                        var metricName = failedMetric.MetricPoint.Name;
                        _logger?.LogWarning("Sink {SinkName} not found or disabled, skipping metric {MetricName}",
                            sinkName, metricName);
                        failedCount++;
                        continue;
                    }

                    // Intentar reexportar la métrica
                    // Crear un registry temporal con la métrica fallida para usar ExportFromRegistryAsync
                    var metricPoint = failedMetric.MetricPoint;
                    var tempRegistry = new MetricRegistry();
                    
                    // Reconstruir la métrica en el registry temporal según su tipo
                    // Convertir Dictionary<string, string> a Dictionary<string, string>? para evitar ambigüedad
                    // Si el diccionario está vacío, pasar null para usar el fast path
                    var metricTags = metricPoint.Tags;
                    Dictionary<string, string>? tags = (metricTags != null && metricTags.Count > 0) ? metricTags : null;
                    double metricValue = metricPoint.Value;
                    
                    switch (metricPoint.Type)
                    {
                        case MetricType.Counter:
                            var counter = tempRegistry.GetOrCreateCounter(metricPoint.Name, "");
                            // Usar parámetros nombrados explícitamente para evitar ambigüedad del compilador
                            counter.Inc(tags: tags, value: metricValue);
                            break;
                        case MetricType.Gauge:
                            var gauge = tempRegistry.GetOrCreateGauge(metricPoint.Name, "");
                            // Usar parámetros nombrados explícitamente para evitar ambigüedad del compilador
                            gauge.Set(tags: tags, value: metricValue);
                            break;
                        case MetricType.Histogram:
                            var histogram = tempRegistry.GetOrCreateHistogram(metricPoint.Name, "");
                            // Usar parámetros nombrados explícitamente para evitar ambigüedad del compilador
                            histogram.Observe(tags: tags, value: metricValue);
                            break;
                    }

                    if (_retryPolicy != null)
                    {
                        var result = await _retryPolicy.ExecuteWithResultAsync<bool>(
                            async () =>
                            {
                                await sink.ExportFromRegistryAsync(tempRegistry, cancellationToken);
                                return true;
                            },
                            cancellationToken);

                        if (result.Success)
                        {
                            successCount++;
                            _logger?.LogDebug("Successfully re-exported metric {MetricName} to {SinkName}",
                                metricPoint.Name, sinkName);
                            
                            // Retornar metadata al pool cuando se procesa exitosamente
                            if (failedMetric.Metadata != null)
                            {
                                CollectionPool.ReturnDictionary(failedMetric.Metadata);
                            }
                        }
                        else
                        {
                            // Si falla de nuevo, volver a agregar a la DLQ
                            // Usar pool para nuevo diccionario de metadata
                            var newMetadata = CollectionPool.RentDictionary();
                            if (failedMetric.Metadata != null)
                            {
                                foreach (var kvp in failedMetric.Metadata)
                                {
                                    newMetadata[kvp.Key] = kvp.Value;
                                }
                            }
                            newMetadata["reprocess_attempt"] = (failedMetric.RetryCount + 1).ToString();
                            newMetadata["last_reprocess_at"] = DateTime.UtcNow.ToString("O");
                            
                            // Retornar metadata anterior al pool si existe
                            if (failedMetric.Metadata != null)
                            {
                                CollectionPool.ReturnDictionary(failedMetric.Metadata);
                            }
                            
                            _deadLetterQueue.Enqueue(new FailedMetric(
                                failedMetric.MetricPoint,
                                failedMetric.SinkName,
                                failedMetric.RetryCount + 1,
                                result.LastException,
                                newMetadata));
                            failedCount++;
                            _logger?.LogWarning("Failed to re-export metric {MetricName} to {SinkName} after retry",
                                metricPoint.Name, sinkName);
                        }
                    }
                    else
                    {
                        // Sin retry policy, intentar directamente
                        try
                        {
                            await sink.ExportFromRegistryAsync(tempRegistry, cancellationToken);
                            successCount++;
                            _logger?.LogDebug("Successfully re-exported metric {MetricName} to {SinkName}",
                                metricPoint.Name, sinkName);
                            
                            // Retornar metadata al pool cuando se procesa exitosamente
                            if (failedMetric.Metadata != null)
                            {
                                CollectionPool.ReturnDictionary(failedMetric.Metadata);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Si falla, volver a agregar a la DLQ
                            // Usar pool para nuevo diccionario de metadata
                            var newMetadata = CollectionPool.RentDictionary();
                            if (failedMetric.Metadata != null)
                            {
                                foreach (var kvp in failedMetric.Metadata)
                                {
                                    newMetadata[kvp.Key] = kvp.Value;
                                }
                            }
                            newMetadata["reprocess_attempt"] = (failedMetric.RetryCount + 1).ToString();
                            newMetadata["last_reprocess_at"] = DateTime.UtcNow.ToString("O");
                            
                            // Retornar metadata anterior al pool si existe
                            if (failedMetric.Metadata != null)
                            {
                                CollectionPool.ReturnDictionary(failedMetric.Metadata);
                            }
                            
                            _deadLetterQueue.Enqueue(new FailedMetric(
                                failedMetric.MetricPoint,
                                failedMetric.SinkName,
                                failedMetric.RetryCount + 1,
                                ex,
                                newMetadata));
                            failedCount++;
                            _logger?.LogWarning(ex, "Failed to re-export metric {MetricName} to {SinkName}",
                                metricPoint.Name, sinkName);
                        }
                    }

                    processedCount++;
                }
                catch (Exception ex)
                {
                    var metricName = "unknown";
                    try
                    {
                        if (failedMetric != null)
                        {
                            metricName = failedMetric.MetricPoint.Name;
                        }
                    }
                    catch
                    {
                        // Ignorar si falla al acceder al nombre
                    }
                    
                    _logger?.LogError(ex, "Error processing failed metric {MetricName}", metricName);
                    failedCount++;
                }
            }

            _logger?.LogInformation("DLQ processing completed. Processed: {Processed}, Success: {Success}, Failed: {Failed}",
                processedCount, successCount, failedCount);
        }
    }
}
