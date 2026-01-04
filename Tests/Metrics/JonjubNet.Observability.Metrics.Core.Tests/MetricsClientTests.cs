using FluentAssertions;
using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.Core.MetricTypes;
using Xunit;

namespace JonjubNet.Observability.Metrics.Core.Tests
{
    public class MetricsClientTests
    {
        [Fact]
        public void Increment_ShouldCreateCounterAndIncrement()
        {
            // Arrange
            var registry = new MetricRegistry();
            var client = new MetricsClient(registry);

            // Act
            client.Increment("test_counter", 5.0);

            // Assert
            var counter = registry.GetOrCreateCounter("test_counter", "");
            counter.GetValue().Should().Be(5);
        }

        [Fact]
        public void SetGauge_ShouldCreateGaugeAndSetValue()
        {
            // Arrange
            var registry = new MetricRegistry();
            var client = new MetricsClient(registry);

            // Act
            client.SetGauge("test_gauge", 42.5);

            // Assert
            var gauge = registry.GetOrCreateGauge("test_gauge", "");
            gauge.GetValue().Should().Be(42.5);
        }

        [Fact]
        public void ObserveHistogram_ShouldCreateHistogramAndObserve()
        {
            // Arrange
            var registry = new MetricRegistry();
            var client = new MetricsClient(registry);

            // Act
            client.ObserveHistogram("test_histogram", 10.5);

            // Assert
            var histogram = registry.GetOrCreateHistogram("test_histogram", "");
            var data = histogram.GetData();
            data.Should().NotBeNull();
            data!.Count.Should().Be(1);
            data.Sum.Should().Be(10.5);
        }

        [Fact]
        public void CreateCounter_ShouldReturnCounter()
        {
            // Arrange
            var registry = new MetricRegistry();
            var client = new MetricsClient(registry);

            // Act
            var counter = client.CreateCounter("test_counter", "Description");

            // Assert
            counter.Should().NotBeNull();
            counter.Name.Should().Be("test_counter");
            counter.Description.Should().Be("Description");
        }

        [Fact]
        public void Increment_WithTags_ShouldIncludeTags()
        {
            // Arrange
            var registry = new MetricRegistry();
            var client = new MetricsClient(registry);
            var tags = new Dictionary<string, string> { ["env"] = "prod" };

            // Act
            client.Increment("test_counter", 1.0, tags);

            // Assert
            var counter = registry.GetOrCreateCounter("test_counter", "");
            counter.GetValue(tags).Should().Be(1);
        }

        [Fact]
        public void RecordHistogram_ShouldDelegateToObserveHistogram()
        {
            // Arrange
            var registry = new MetricRegistry();
            var client = new MetricsClient(registry);

            // Act
            client.RecordHistogram("test_histogram", 15.5);

            // Assert
            var histogram = registry.GetOrCreateHistogram("test_histogram", "");
            var data = histogram.GetData();
            data.Should().NotBeNull();
            data!.Count.Should().Be(1);
            data.Sum.Should().Be(15.5);
        }

        [Fact]
        public void StartTimer_ShouldReturnDisposableTimer()
        {
            // Arrange
            var registry = new MetricRegistry();
            var client = new MetricsClient(registry);

            // Act
            using var timer = client.StartTimer("test_timer");

            // Assert
            timer.Should().NotBeNull();
            System.Threading.Thread.Sleep(10); // Pequeña pausa para que el timer registre algo
        }

        [Fact]
        public void StartTimer_WithTags_ShouldWork()
        {
            // Arrange
            var registry = new MetricRegistry();
            var client = new MetricsClient(registry);
            var tags = new Dictionary<string, string> { ["env"] = "test" };

            // Act
            using var timer = client.StartTimer("test_timer", tags);

            // Assert
            timer.Should().NotBeNull();
            System.Threading.Thread.Sleep(10);
        }

        [Fact]
        public void CreateSlidingWindowSummary_ShouldReturnSlidingWindowSummary()
        {
            // Arrange
            var registry = new MetricRegistry();
            var client = new MetricsClient(registry);
            var windowSize = TimeSpan.FromMinutes(5);

            // Act
            var summary = client.CreateSlidingWindowSummary("test_sliding", "Description", windowSize);

            // Assert
            summary.Should().NotBeNull();
            summary.Name.Should().Be("test_sliding");
            summary.Description.Should().Be("Description");
            summary.WindowSize.Should().Be(windowSize);
        }

        [Fact]
        public void ObserveSlidingWindowSummary_ShouldObserveValue()
        {
            // Arrange
            var registry = new MetricRegistry();
            var client = new MetricsClient(registry);
            var windowSize = TimeSpan.FromMinutes(5);

            // Act
            client.ObserveSlidingWindowSummary("test_sliding", windowSize, 42.5);

            // Assert
            var summary = registry.GetOrCreateSlidingWindowSummary("test_sliding", "", windowSize);
            summary.Should().NotBeNull();
        }

        [Fact]
        public void AddToAggregator_ShouldAddValue()
        {
            // Arrange
            var registry = new MetricRegistry();
            var client = new MetricsClient(registry);

            // Act
            client.AddToAggregator("test_metric", 10.0);

            // Assert
            var value = client.GetAggregatedValue("test_metric", Core.Aggregation.AggregationType.Sum);
            value.Should().Be(10.0);
        }

        [Fact]
        public void GetAggregatedValue_ShouldReturnAggregatedValue()
        {
            // Arrange
            var registry = new MetricRegistry();
            var client = new MetricsClient(registry);
            client.AddToAggregator("test_metric", 10.0);
            client.AddToAggregator("test_metric", 20.0);

            // Act
            var sum = client.GetAggregatedValue("test_metric", Core.Aggregation.AggregationType.Sum);

            // Assert
            sum.Should().Be(30.0);
        }

        [Fact]
        public void GetAggregatedStats_ShouldReturnStats()
        {
            // Arrange
            var registry = new MetricRegistry();
            var client = new MetricsClient(registry);
            client.AddToAggregator("test_metric", 10.0);
            client.AddToAggregator("test_metric", 20.0);

            // Act
            var stats = client.GetAggregatedStats("test_metric");

            // Assert
            stats.Should().NotBeNull();
            stats!.Count.Should().Be(2);
        }
    }
}

