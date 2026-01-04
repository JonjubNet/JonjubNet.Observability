using TraceFlushScheduler = JonjubNet.Observability.Tracing.Core.TraceFlushScheduler;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Hosting
{
    /// <summary>
    /// Background service que ejecuta el TraceFlushScheduler
    /// Similar a MetricsBackgroundService y LoggingBackgroundService pero para traces
    /// </summary>
    public class TracingBackgroundService : BackgroundService
    {
        private readonly TraceFlushScheduler _scheduler;
        private readonly ILogger<TracingBackgroundService>? _logger;

        public TracingBackgroundService(
            TraceFlushScheduler scheduler,
            ILogger<TracingBackgroundService>? logger = null)
        {
            _scheduler = scheduler;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger?.LogInformation("Starting TracingBackgroundService");
            _scheduler.Start();
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Stopping TracingBackgroundService");
            _scheduler.Dispose();
            await base.StopAsync(cancellationToken);
        }
    }
}

