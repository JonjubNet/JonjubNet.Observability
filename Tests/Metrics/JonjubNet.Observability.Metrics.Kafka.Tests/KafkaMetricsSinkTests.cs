using FluentAssertions;
using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace JonjubNet.Observability.Metrics.Kafka.Tests
{
    public class KafkaMetricsSinkTests
    {
        [Fact]
        public void Constructor_ShouldInitialize()
        {
            // Arrange
            var options = Options.Create(new KafkaOptions { Enabled = true });

            // Act
            var sink = new KafkaMetricsSink(options);

            // Assert
            sink.Should().NotBeNull();
            sink.Name.Should().Be("Kafka");
            sink.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void Constructor_WhenDisabled_ShouldNotBeEnabled()
        {
            // Arrange
            var options = Options.Create(new KafkaOptions { Enabled = false });

            // Act
            var sink = new KafkaMetricsSink(options);

            // Assert
            sink.IsEnabled.Should().BeFalse();
        }

        [Fact]
        public async Task ExportFromRegistryAsync_WhenDisabled_ShouldCompleteImmediately()
        {
            // Arrange
            var registry = new MetricRegistry();
            var options = Options.Create(new KafkaOptions { Enabled = false });
            var sink = new KafkaMetricsSink(options);

            // Act
            await sink.ExportFromRegistryAsync(registry);

            // Assert
            // Should complete without errors
        }
    }
}

