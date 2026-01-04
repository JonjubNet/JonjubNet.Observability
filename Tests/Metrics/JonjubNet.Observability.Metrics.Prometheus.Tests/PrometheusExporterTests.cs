using FluentAssertions;
using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.Prometheus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace JonjubNet.Observability.Metrics.Prometheus.Tests
{
    public class PrometheusExporterTests
    {
        [Fact]
        public void Constructor_ShouldInitialize()
        {
            // Arrange
            var registry = new MetricRegistry();
            var formatter = new PrometheusFormatter();
            var options = Options.Create(new PrometheusOptions { Enabled = true });

            // Act
            var exporter = new PrometheusExporter(registry, formatter, options);

            // Assert
            exporter.Should().NotBeNull();
            exporter.Name.Should().Be("Prometheus");
            exporter.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void ExportFromRegistryAsync_ShouldComplete()
        {
            // Arrange
            var registry = new MetricRegistry();
            var formatter = new PrometheusFormatter();
            var options = Options.Create(new PrometheusOptions { Enabled = true });
            var exporter = new PrometheusExporter(registry, formatter, options);

            // Act
            var task = exporter.ExportFromRegistryAsync(registry);

            // Assert
            task.IsCompletedSuccessfully.Should().BeTrue();
        }

        [Fact]
        public void GetMetricsText_ShouldReturnFormattedText()
        {
            // Arrange
            var registry = new MetricRegistry();
            var formatter = new PrometheusFormatter();
            var options = Options.Create(new PrometheusOptions { Enabled = true });
            var exporter = new PrometheusExporter(registry, formatter, options);
            var counter = registry.GetOrCreateCounter("test_counter", "Test");
            counter.Inc(value: 1.0);

            // Act
            var result = exporter.GetMetricsText();

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("test_counter");
        }
    }
}

