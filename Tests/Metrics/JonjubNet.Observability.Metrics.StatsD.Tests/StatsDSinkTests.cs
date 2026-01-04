using FluentAssertions;
using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.StatsD;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace JonjubNet.Observability.Metrics.StatsD.Tests
{
    public class StatsDSinkTests
    {
        [Fact]
        public void Constructor_ShouldInitialize()
        {
            // Arrange
            var options = Options.Create(new StatsDOptions { Enabled = true });

            // Act
            var sink = new StatsDSink(options);

            // Assert
            sink.Should().NotBeNull();
            sink.Name.Should().Be("StatsD");
            sink.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void Constructor_WhenDisabled_ShouldNotBeEnabled()
        {
            // Arrange
            var options = Options.Create(new StatsDOptions { Enabled = false });

            // Act
            var sink = new StatsDSink(options);

            // Assert
            sink.IsEnabled.Should().BeFalse();
        }

        [Fact]
        public async Task ExportFromRegistryAsync_WhenDisabled_ShouldCompleteImmediately()
        {
            // Arrange
            var registry = new MetricRegistry();
            var options = Options.Create(new StatsDOptions { Enabled = false });
            var sink = new StatsDSink(options);

            // Act
            await sink.ExportFromRegistryAsync(registry);

            // Assert
            // Should complete without errors
        }
    }
}

