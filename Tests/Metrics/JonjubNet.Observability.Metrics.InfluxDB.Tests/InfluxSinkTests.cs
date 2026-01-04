using FluentAssertions;
using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.InfluxDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace JonjubNet.Observability.Metrics.InfluxDB.Tests
{
    public class InfluxSinkTests
    {
        [Fact]
        public void Constructor_ShouldInitialize()
        {
            // Arrange
            var options = Options.Create(new InfluxOptions { Enabled = true });

            // Act
            var sink = new InfluxSink(options);

            // Assert
            sink.Should().NotBeNull();
            sink.Name.Should().Be("InfluxDB");
            sink.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void Constructor_WhenDisabled_ShouldNotBeEnabled()
        {
            // Arrange
            var options = Options.Create(new InfluxOptions { Enabled = false });

            // Act
            var sink = new InfluxSink(options);

            // Assert
            sink.IsEnabled.Should().BeFalse();
        }

        [Fact]
        public async Task ExportFromRegistryAsync_WhenDisabled_ShouldCompleteImmediately()
        {
            // Arrange
            var registry = new MetricRegistry();
            var options = Options.Create(new InfluxOptions { Enabled = false });
            var sink = new InfluxSink(options);

            // Act
            await sink.ExportFromRegistryAsync(registry);

            // Assert
            // Should complete without errors
        }
    }
}

