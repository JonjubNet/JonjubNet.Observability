using JonjubNet.Observability.Metrics.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JonjubNet.Observability.Hosting
{
    /// <summary>
    /// Background service que ejecuta el MetricFlushScheduler
    /// </summary>
    public class MetricsBackgroundService : BackgroundService
    {
        private readonly MetricFlushScheduler _scheduler;
        private readonly ILogger<MetricsBackgroundService>? _logger;

        public MetricsBackgroundService(
            MetricFlushScheduler scheduler,
            ILogger<MetricsBackgroundService>? logger = null)
        {
            _scheduler = scheduler;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger?.LogInformation("Starting MetricsBackgroundService");
            _scheduler.Start();
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Stopping MetricsBackgroundService");
            _scheduler.Dispose();
            await base.StopAsync(cancellationToken);
        }
    }
}

