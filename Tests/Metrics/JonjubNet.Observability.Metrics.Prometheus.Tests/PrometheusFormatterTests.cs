using FluentAssertions;
using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.Prometheus;
using Xunit;

namespace JonjubNet.Observability.Metrics.Prometheus.Tests
{
    public class PrometheusFormatterTests
    {
        [Fact]
        public void FormatRegistry_WithCounter_ShouldFormatCorrectly()
        {
            // Arrange
            var registry = new MetricRegistry();
            var formatter = new PrometheusFormatter();
            var counter = registry.GetOrCreateCounter("test_counter", "Test counter");
            counter.Inc(value: 5.0);

            // Act
            var result = formatter.FormatRegistry(registry);

            // Assert
            result.Should().Contain("test_counter");
            result.Should().Contain("counter");
            result.Should().Contain("5");
        }

        [Fact]
        public void FormatRegistry_WithGauge_ShouldFormatCorrectly()
        {
            // Arrange
            var registry = new MetricRegistry();
            var formatter = new PrometheusFormatter();
            var gauge = registry.GetOrCreateGauge("test_gauge", "Test gauge");
            gauge.Set(value: 42.5);

            // Act
            var result = formatter.FormatRegistry(registry);

            // Assert
            result.Should().Contain("test_gauge");
            result.Should().Contain("gauge");
            result.Should().Contain("42.5");
        }

        [Fact]
        public void FormatRegistry_WithHistogram_ShouldFormatCorrectly()
        {
            // Arrange
            var registry = new MetricRegistry();
            var formatter = new PrometheusFormatter();
            var histogram = registry.GetOrCreateHistogram("test_histogram", "Test histogram");
            histogram.Observe(value: 10.5);

            // Act
            var result = formatter.FormatRegistry(registry);

            // Assert
            result.Should().Contain("test_histogram");
            result.Should().Contain("histogram");
        }

        [Fact]
        public void Format_ShouldReturnPrometheus()
        {
            // Arrange
            var formatter = new PrometheusFormatter();

            // Act
            var format = formatter.Format;

            // Assert
            format.Should().Be("Prometheus");
        }
    }
}

