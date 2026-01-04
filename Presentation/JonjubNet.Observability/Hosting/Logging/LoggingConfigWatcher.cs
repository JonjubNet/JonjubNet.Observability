using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LoggingOptions = JonjubNet.Observability.Logging.Shared.Configuration.LoggingOptions;

namespace JonjubNet.Observability.Hosting
{
    /// <summary>
    /// BackgroundService que monitorea cambios en la configuración de logging
    /// Detecta cambios en appsettings.json y los registra en logs
    /// Similar a MetricsConfigWatcher pero para logs
    /// </summary>
    public class LoggingConfigWatcher : BackgroundService
    {
        private readonly IOptionsMonitor<LoggingOptions> _optionsMonitor;
        private readonly ILogger<LoggingConfigWatcher> _logger;

        public LoggingConfigWatcher(
            IOptionsMonitor<LoggingOptions> optionsMonitor,
            ILogger<LoggingConfigWatcher> logger)
        {
            _optionsMonitor = optionsMonitor;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _optionsMonitor.OnChange(options =>
            {
                _logger.LogInformation("=== Configuración de Logging Cambiada ===");
                _logger.LogInformation($"Habilitado: {options.Enabled}");
                _logger.LogInformation($"ServiceName: {options.ServiceName}");
                _logger.LogInformation($"Environment: {options.Environment}");
                _logger.LogInformation($"Flush Interval: {options.FlushIntervalMs}ms");
                _logger.LogInformation($"Batch Size: {options.BatchSize}");
                _logger.LogInformation($"DeadLetterQueue Enabled: {options.DeadLetterQueue.Enabled}");
                _logger.LogInformation($"RetryPolicy Enabled: {options.RetryPolicy.Enabled}");
                _logger.LogInformation($"CircuitBreaker Enabled: {options.CircuitBreaker.Enabled}");
                _logger.LogInformation($"Encryption Enabled: {options.Encryption.Enabled}");
                _logger.LogInformation($"Sampling Enabled: {options.Sampling.Enabled}");
                _logger.LogInformation($"DataSanitization Enabled: {options.DataSanitization.Enabled}");
            });

            return Task.CompletedTask;
        }
    }
}

