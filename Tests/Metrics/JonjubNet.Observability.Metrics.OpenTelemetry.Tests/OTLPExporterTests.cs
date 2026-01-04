using FluentAssertions;
using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.OpenTelemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace JonjubNet.Observability.Metrics.OpenTelemetry.Tests
{
    public class OTLPExporterTests
    {
        [Fact]
        public void Constructor_ShouldInitialize()
        {
            // Arrange
            var options = Options.Create(new OTLOptions { Enabled = true });

            // Act
            var exporter = new OTLPExporter(options);

            // Assert
            exporter.Should().NotBeNull();
            exporter.Name.Should().Be("OpenTelemetry");
            exporter.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void Constructor_WhenDisabled_ShouldNotBeEnabled()
        {
            // Arrange
            var options = Options.Create(new OTLOptions { Enabled = false });

            // Act
            var exporter = new OTLPExporter(options);

            // Assert
            exporter.IsEnabled.Should().BeFalse();
        }

        [Fact]
        public async Task ExportFromRegistryAsync_WhenDisabled_ShouldCompleteImmediately()
        {
            // Arrange
            var registry = new MetricRegistry();
            var options = Options.Create(new OTLOptions { Enabled = false });
            var exporter = new OTLPExporter(options);

            // Act
            await exporter.ExportFromRegistryAsync(registry);

            // Assert
            // Should complete without errors
        }
    }
}

