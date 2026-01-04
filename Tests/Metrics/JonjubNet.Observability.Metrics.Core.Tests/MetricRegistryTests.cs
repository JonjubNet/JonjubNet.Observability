using FluentAssertions;
using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.Core.MetricTypes;
using Xunit;

namespace JonjubNet.Observability.Metrics.Core.Tests
{
    public class MetricRegistryTests
    {
        [Fact]
        public void GetOrCreateCounter_ShouldCreateNewCounter()
        {
            // Arrange
            var registry = new MetricRegistry();

            // Act
            var counter = registry.GetOrCreateCounter("test_counter", "Description");

            // Assert
            counter.Should().NotBeNull();
            counter.Name.Should().Be("test_counter");
            counter.Description.Should().Be("Description");
        }

        [Fact]
        public void GetOrCreateCounter_SameName_ShouldReturnSameInstance()
        {
            // Arrange
            var registry = new MetricRegistry();

            // Act
            var counter1 = registry.GetOrCreateCounter("test_counter", "Description");
            var counter2 = registry.GetOrCreateCounter("test_counter", "Description");

            // Assert
            counter1.Should().BeSameAs(counter2);
        }

        [Fact]
        public void GetOrCreateGauge_ShouldCreateNewGauge()
        {
            // Arrange
            var registry = new MetricRegistry();

            // Act
            var gauge = registry.GetOrCreateGauge("test_gauge", "Description");

            // Assert
            gauge.Should().NotBeNull();
            gauge.Name.Should().Be("test_gauge");
            gauge.Description.Should().Be("Description");
        }

        [Fact]
        public void GetOrCreateGauge_SameName_ShouldReturnSameInstance()
        {
            // Arrange
            var registry = new MetricRegistry();

            // Act
            var gauge1 = registry.GetOrCreateGauge("test_gauge", "Description");
            var gauge2 = registry.GetOrCreateGauge("test_gauge", "Description");

            // Assert
            gauge1.Should().BeSameAs(gauge2);
        }

        [Fact]
        public void GetOrCreateHistogram_ShouldCreateNewHistogram()
        {
            // Arrange
            var registry = new MetricRegistry();
            var buckets = new double[] { 0.1, 0.5, 1.0 };

            // Act
            var histogram = registry.GetOrCreateHistogram("test_histogram", "Description", buckets);

            // Assert
            histogram.Should().NotBeNull();
            histogram.Name.Should().Be("test_histogram");
            histogram.Description.Should().Be("Description");
        }

        [Fact]
        public void GetOrCreateSummary_ShouldCreateNewSummary()
        {
            // Arrange
            var registry = new MetricRegistry();
            var quantiles = new double[] { 0.5, 0.95, 0.99 };

            // Act
            var summary = registry.GetOrCreateSummary("test_summary", "Description", quantiles);

            // Assert
            summary.Should().NotBeNull();
            summary.Name.Should().Be("test_summary");
            summary.Description.Should().Be("Description");
        }

        [Fact]
        public void GetAllCounters_ShouldReturnAllCounters()
        {
            // Arrange
            var registry = new MetricRegistry();
            registry.GetOrCreateCounter("counter1", "Desc1");
            registry.GetOrCreateCounter("counter2", "Desc2");

            // Act
            var counters = registry.GetAllCounters();

            // Assert
            counters.Should().HaveCount(2);
            counters.Should().ContainKey("counter1");
            counters.Should().ContainKey("counter2");
        }

        [Fact]
        public void GetAllGauges_ShouldReturnAllGauges()
        {
            // Arrange
            var registry = new MetricRegistry();
            registry.GetOrCreateGauge("gauge1", "Desc1");
            registry.GetOrCreateGauge("gauge2", "Desc2");

            // Act
            var gauges = registry.GetAllGauges();

            // Assert
            gauges.Should().HaveCount(2);
            gauges.Should().ContainKey("gauge1");
            gauges.Should().ContainKey("gauge2");
        }

        [Fact]
        public void Clear_ShouldRemoveAllMetrics()
        {
            // Arrange
            var registry = new MetricRegistry();
            registry.GetOrCreateCounter("counter1", "Desc1");
            registry.GetOrCreateGauge("gauge1", "Desc1");

            // Act
            registry.Clear();

            // Assert
            registry.GetAllCounters().Should().BeEmpty();
            registry.GetAllGauges().Should().BeEmpty();
            registry.GetAllHistograms().Should().BeEmpty();
            registry.GetAllSummaries().Should().BeEmpty();
        }

        [Fact]
        public void GetOrCreateHistogram_WithBuckets_ShouldCreateHistogramWithBuckets()
        {
            // Arrange
            var registry = new MetricRegistry();
            var buckets = new double[] { 0.1, 0.5, 1.0, 2.0 };

            // Act
            var histogram = registry.GetOrCreateHistogram("test_histogram", "Description", buckets);

            // Assert
            histogram.Should().NotBeNull();
            histogram.Name.Should().Be("test_histogram");
        }

        [Fact]
        public void GetOrCreateHistogram_SameName_ShouldReturnSameInstance()
        {
            // Arrange
            var registry = new MetricRegistry();

            // Act
            var histogram1 = registry.GetOrCreateHistogram("test_histogram", "Description");
            var histogram2 = registry.GetOrCreateHistogram("test_histogram", "Description");

            // Assert
            histogram1.Should().BeSameAs(histogram2);
        }

        [Fact]
        public void GetOrCreateSummary_WithQuantiles_ShouldCreateSummaryWithQuantiles()
        {
            // Arrange
            var registry = new MetricRegistry();
            var quantiles = new double[] { 0.5, 0.95, 0.99 };

            // Act
            var summary = registry.GetOrCreateSummary("test_summary", "Description", quantiles);

            // Assert
            summary.Should().NotBeNull();
            summary.Name.Should().Be("test_summary");
        }

        [Fact]
        public void GetOrCreateSummary_SameName_ShouldReturnSameInstance()
        {
            // Arrange
            var registry = new MetricRegistry();

            // Act
            var summary1 = registry.GetOrCreateSummary("test_summary", "Description");
            var summary2 = registry.GetOrCreateSummary("test_summary", "Description");

            // Assert
            summary1.Should().BeSameAs(summary2);
        }

        [Fact]
        public void GetAllHistograms_ShouldReturnAllHistograms()
        {
            // Arrange
            var registry = new MetricRegistry();
            registry.GetOrCreateHistogram("histogram1", "Desc1");
            registry.GetOrCreateHistogram("histogram2", "Desc2");

            // Act
            var histograms = registry.GetAllHistograms();

            // Assert
            histograms.Should().HaveCount(2);
            histograms.Should().ContainKey("histogram1");
            histograms.Should().ContainKey("histogram2");
        }

        [Fact]
        public void GetAllSummaries_ShouldReturnAllSummaries()
        {
            // Arrange
            var registry = new MetricRegistry();
            registry.GetOrCreateSummary("summary1", "Desc1");
            registry.GetOrCreateSummary("summary2", "Desc2");

            // Act
            var summaries = registry.GetAllSummaries();

            // Assert
            summaries.Should().HaveCount(2);
            summaries.Should().ContainKey("summary1");
            summaries.Should().ContainKey("summary2");
        }

        [Fact]
        public void GetOrCreateSlidingWindowSummary_ShouldCreateNewSummary()
        {
            // Arrange
            var registry = new MetricRegistry();
            var windowSize = TimeSpan.FromMinutes(5);
            var quantiles = new double[] { 0.5, 0.95 };

            // Act
            var summary = registry.GetOrCreateSlidingWindowSummary("test_sliding", "Description", windowSize, quantiles);

            // Assert
            summary.Should().NotBeNull();
            summary.Name.Should().Be("test_sliding");
            summary.Description.Should().Be("Description");
            summary.WindowSize.Should().Be(windowSize);
        }

        [Fact]
        public void GetOrCreateSlidingWindowSummary_SameName_ShouldReturnSameInstance()
        {
            // Arrange
            var registry = new MetricRegistry();
            var windowSize = TimeSpan.FromMinutes(5);

            // Act
            var summary1 = registry.GetOrCreateSlidingWindowSummary("test_sliding", "Description", windowSize);
            var summary2 = registry.GetOrCreateSlidingWindowSummary("test_sliding", "Description", windowSize);

            // Assert
            summary1.Should().BeSameAs(summary2);
        }

        [Fact]
        public void GetAllSlidingWindowSummaries_ShouldReturnAllSummaries()
        {
            // Arrange
            var registry = new MetricRegistry();
            registry.GetOrCreateSlidingWindowSummary("sliding1", "Desc1", TimeSpan.FromMinutes(5));
            registry.GetOrCreateSlidingWindowSummary("sliding2", "Desc2", TimeSpan.FromMinutes(10));

            // Act
            var summaries = registry.GetAllSlidingWindowSummaries();

            // Assert
            summaries.Should().HaveCount(2);
            summaries.Should().ContainKey("sliding1");
            summaries.Should().ContainKey("sliding2");
        }

        [Fact]
        public void Aggregator_ShouldReturnMetricAggregator()
        {
            // Arrange
            var registry = new MetricRegistry();

            // Act
            var aggregator = registry.Aggregator;

            // Assert
            aggregator.Should().NotBeNull();
        }

        [Fact]
        public void Clear_ShouldAlsoClearSlidingWindowSummaries()
        {
            // Arrange
            var registry = new MetricRegistry();
            registry.GetOrCreateSlidingWindowSummary("sliding1", "Desc1", TimeSpan.FromMinutes(5));

            // Act
            registry.Clear();

            // Assert
            registry.GetAllSlidingWindowSummaries().Should().BeEmpty();
        }
    }
}

