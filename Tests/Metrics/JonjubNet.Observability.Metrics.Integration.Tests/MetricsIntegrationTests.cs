using FluentAssertions;
using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.Core.Interfaces;
using JonjubNet.Observability.Metrics.Prometheus;
using JonjubNet.Observability.Metrics.Shared.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace JonjubNet.Observability.Metrics.Integration.Tests
{
    public class MetricsIntegrationTests
    {
        [Fact]
        public void MetricsClient_WithPrometheusExporter_ShouldWork()
        {
            // Arrange
            var registry = new MetricRegistry();
            var client = new MetricsClient(registry);
            
            var prometheusOptions = Options.Create(new PrometheusOptions { Enabled = true });
            var formatter = new PrometheusFormatter();
            var exporter = new PrometheusExporter(registry, formatter, prometheusOptions);

            // Act
            client.Increment("test_counter", 5.0);
            client.SetGauge("test_gauge", 42.5);

            // Assert
            var counter = registry.GetOrCreateCounter("test_counter", "");
            counter.GetValue().Should().Be(5);

            var gauge = registry.GetOrCreateGauge("test_gauge", "");
            gauge.GetValue().Should().Be(42.5);

            // Prometheus formatter should work
            var metricsText = formatter.FormatRegistry(registry);
            metricsText.Should().Contain("test_counter");
            metricsText.Should().Contain("test_gauge");
        }

        [Fact]
        public void MetricRegistry_WithMultipleTypes_ShouldTrackAll()
        {
            // Arrange
            var registry = new MetricRegistry();

            // Act
            var counter = registry.GetOrCreateCounter("counter1", "Desc");
            var gauge = registry.GetOrCreateGauge("gauge1", "Desc");
            var histogram = registry.GetOrCreateHistogram("histogram1", "Desc");
            var summary = registry.GetOrCreateSummary("summary1", "Desc");

            counter.Inc(value: 10.0);
            gauge.Set(value: 20.0);
            histogram.Observe(value: 30.0);
            summary.Observe(value: 40.0);

            // Assert
            registry.GetAllCounters().Should().HaveCount(1);
            registry.GetAllGauges().Should().HaveCount(1);
            registry.GetAllHistograms().Should().HaveCount(1);
            registry.GetAllSummaries().Should().HaveCount(1);
        }
    }
}

