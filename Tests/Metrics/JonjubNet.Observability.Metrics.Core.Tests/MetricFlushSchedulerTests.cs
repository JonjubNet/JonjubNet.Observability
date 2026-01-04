using FluentAssertions;
using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JonjubNet.Observability.Metrics.Core.Tests
{
    public class MetricFlushSchedulerTests
    {
        [Fact]
        public void Constructor_ShouldInitialize()
        {
            // Arrange
            var registry = new MetricRegistry();
            var sinks = new List<IMetricsSink>();
            var logger = new Mock<ILogger<MetricFlushScheduler>>();

            // Act
            var scheduler = new MetricFlushScheduler(registry, sinks, exportInterval: TimeSpan.FromMilliseconds(100), logger: logger.Object);

            // Assert
            scheduler.Should().NotBeNull();
        }

        [Fact]
        public void Start_ShouldStartBackgroundTask()
        {
            // Arrange
            var registry = new MetricRegistry();
            var sinks = new List<IMetricsSink>();
            var logger = new Mock<ILogger<MetricFlushScheduler>>();
            var scheduler = new MetricFlushScheduler(registry, sinks, exportInterval: TimeSpan.FromMilliseconds(100), logger: logger.Object);

            // Act
            scheduler.Start();

            // Assert
            scheduler.Should().NotBeNull();
            // Give it a moment to start
            System.Threading.Thread.Sleep(50);
        }

        [Fact]
        public void Dispose_ShouldStopScheduler()
        {
            // Arrange
            var registry = new MetricRegistry();
            var sinks = new List<IMetricsSink>();
            var logger = new Mock<ILogger<MetricFlushScheduler>>();
            var scheduler = new MetricFlushScheduler(registry, sinks, exportInterval: TimeSpan.FromMilliseconds(100), logger: logger.Object);
            scheduler.Start();

            // Act
            scheduler.Dispose();

            // Assert
            // Should not throw
            scheduler.Should().NotBeNull();
        }
    }
}

