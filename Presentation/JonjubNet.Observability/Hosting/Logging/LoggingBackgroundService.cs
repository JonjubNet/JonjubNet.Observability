using LogFlushScheduler = JonjubNet.Observability.Logging.Core.LogFlushScheduler;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Hosting
{
    /// <summary>
    /// Background service que ejecuta el LogFlushScheduler
    /// Similar a MetricsBackgroundService pero para logs
    /// </summary>
    public class LoggingBackgroundService : BackgroundService
    {
        private readonly LogFlushScheduler _scheduler;
        private readonly ILogger<LoggingBackgroundService>? _logger;

        public LoggingBackgroundService(
            LogFlushScheduler scheduler,
            ILogger<LoggingBackgroundService>? logger = null)
        {
            _scheduler = scheduler;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger?.LogInformation("Starting LoggingBackgroundService");
            _scheduler.Start();
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Stopping LoggingBackgroundService");
            _scheduler.Dispose();
            await base.StopAsync(cancellationToken);
        }
    }
}

