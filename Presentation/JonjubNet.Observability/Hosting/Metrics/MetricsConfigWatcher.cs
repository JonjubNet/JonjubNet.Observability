using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using JonjubNet.Observability.Metrics.Shared.Configuration;

namespace JonjubNet.Observability.Hosting
{
    /// <summary>
    /// BackgroundService que monitorea cambios en la configuración de métricas (Paso 7.5).
    /// Detecta cambios en appsettings.json y los registra en logs.
    /// </summary>
    public class MetricsConfigWatcher : BackgroundService
    {
        private readonly IOptionsMonitor<MetricsOptions> _optionsMonitor;
        private readonly ILogger<MetricsConfigWatcher> _logger;

        public MetricsConfigWatcher(
            IOptionsMonitor<MetricsOptions> optionsMonitor,
            ILogger<MetricsConfigWatcher> logger)
        {
            _optionsMonitor = optionsMonitor;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _optionsMonitor.OnChange(options =>
            {
                _logger.LogInformation("=== Configuración de Métricas Cambiada ===");
                _logger.LogInformation($"Habilitado: {options.Enabled}");
                _logger.LogInformation($"ServiceName: {options.ServiceName}");
                _logger.LogInformation($"Environment: {options.Environment}");
                _logger.LogInformation($"Flush Interval: {options.FlushIntervalMs}ms");
                _logger.LogInformation($"Batch Size: {options.BatchSize}");
                _logger.LogInformation($"DeadLetterQueue Enabled: {options.DeadLetterQueue.Enabled}");
                _logger.LogInformation($"RetryPolicy Enabled: {options.RetryPolicy.Enabled}");
                _logger.LogInformation($"CircuitBreaker Enabled: {options.CircuitBreaker.Enabled}");
                _logger.LogInformation($"Encryption InTransit: {options.Encryption.EnableInTransit}");
                _logger.LogInformation($"Encryption AtRest: {options.Encryption.EnableAtRest}");
            });

            return Task.CompletedTask;
        }
    }
}

