using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JonjubNet.Observability.Metrics.Shared.Configuration
{
    /// <summary>
    /// Servicio de hot-reload para configuración de métricas
    /// </summary>
    public class MetricsHotReload : IHostedService
    {
        private readonly IOptionsMonitor<MetricsOptions> _optionsMonitor;
        private readonly MetricsConfigurationManager _configManager;
        private readonly ILogger<MetricsHotReload>? _logger;
        private IDisposable? _optionsChangeToken;

        public MetricsHotReload(
            IOptionsMonitor<MetricsOptions> optionsMonitor,
            MetricsConfigurationManager configManager,
            ILogger<MetricsHotReload>? logger = null)
        {
            _optionsMonitor = optionsMonitor;
            _configManager = configManager;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _optionsChangeToken = _optionsMonitor.OnChange(options =>
            {
                _configManager.Reload();
                _logger?.LogInformation("Metrics configuration hot-reloaded");
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _optionsChangeToken?.Dispose();
            return Task.CompletedTask;
        }
    }
}

