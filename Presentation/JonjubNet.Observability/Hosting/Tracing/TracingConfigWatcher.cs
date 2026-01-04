using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TracingOptions = JonjubNet.Observability.Tracing.Shared.Configuration.TracingOptions;

namespace JonjubNet.Observability.Hosting
{
    /// <summary>
    /// BackgroundService que monitorea cambios en la configuración de tracing
    /// Detecta cambios en appsettings.json y los registra en logs
    /// Similar a MetricsConfigWatcher y LoggingConfigWatcher pero para traces
    /// </summary>
    public class TracingConfigWatcher : BackgroundService
    {
        private readonly IOptionsMonitor<TracingOptions> _optionsMonitor;
        private readonly ILogger<TracingConfigWatcher> _logger;

        public TracingConfigWatcher(
            IOptionsMonitor<TracingOptions> optionsMonitor,
            ILogger<TracingConfigWatcher> logger)
        {
            _optionsMonitor = optionsMonitor;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _optionsMonitor.OnChange(options =>
            {
                _logger.LogInformation("=== Configuración de Tracing Cambiada ===");
                _logger.LogInformation($"Habilitado: {options.Enabled}");
                _logger.LogInformation($"ServiceName: {options.ServiceName}");
                _logger.LogInformation($"Environment: {options.Environment}");
                _logger.LogInformation($"Flush Interval: {options.FlushIntervalMs}ms");
                _logger.LogInformation($"Batch Size: {options.BatchSize}");
                _logger.LogInformation($"DeadLetterQueue Enabled: {options.DeadLetterQueue.Enabled}");
                _logger.LogInformation($"RetryPolicy Enabled: {options.RetryPolicy.Enabled}");
                _logger.LogInformation($"Encryption InTransit Enabled: {options.Encryption.EnableInTransit}");
                _logger.LogInformation($"Encryption TLS Enabled: {options.Encryption.EnableTls}");
            });

            return Task.CompletedTask;
        }
    }
}

