using FluentAssertions;
using JonjubNet.Observability.Metrics.Core.Aggregation;
using Xunit;

namespace JonjubNet.Observability.Metrics.Core.Tests.Aggregation
{
    public class MetricAggregatorTests
    {
        [Fact]
        public void AddValue_ShouldAddValueToMetric()
        {
            // Arrange
            var aggregator = new MetricAggregator();

            // Act
            aggregator.AddValue("test_metric", 10.0);

            // Assert
            var value = aggregator.GetAggregatedValue("test_metric", AggregationType.Sum);
            value.Should().Be(10.0);
        }

        [Fact]
        public void AddValue_WithTags_ShouldCreateSeparateMetric()
        {
            // Arrange
            var aggregator = new MetricAggregator();
            var tags1 = new Dictionary<string, string> { { "env", "test" } };
            var tags2 = new Dictionary<string, string> { { "env", "prod" } };

            // Act
            aggregator.AddValue("test_metric", 10.0, tags1);
            aggregator.AddValue("test_metric", 20.0, tags2);

            // Assert
            var value1 = aggregator.GetAggregatedValue("test_metric", AggregationType.Sum, tags1);
            var value2 = aggregator.GetAggregatedValue("test_metric", AggregationType.Sum, tags2);
            value1.Should().Be(10.0);
            value2.Should().Be(20.0);
        }

        [Fact]
        public void GetAggregatedValue_Sum_ShouldReturnSum()
        {
            // Arrange
            var aggregator = new MetricAggregator();
            aggregator.AddValue("test_metric", 10.0);
            aggregator.AddValue("test_metric", 20.0);
            aggregator.AddValue("test_metric", 30.0);

            // Act
            var sum = aggregator.GetAggregatedValue("test_metric", AggregationType.Sum);

            // Assert
            sum.Should().Be(60.0);
        }

        [Fact]
        public void GetAggregatedValue_Average_ShouldReturnAverage()
        {
            // Arrange
            var aggregator = new MetricAggregator();
            aggregator.AddValue("test_metric", 10.0);
            aggregator.AddValue("test_metric", 20.0);
            aggregator.AddValue("test_metric", 30.0);

            // Act
            var average = aggregator.GetAggregatedValue("test_metric", AggregationType.Average);

            // Assert
            average.Should().Be(20.0);
        }

        [Fact]
        public void GetAggregatedValue_Min_ShouldReturnMinimum()
        {
            // Arrange
            var aggregator = new MetricAggregator();
            aggregator.AddValue("test_metric", 30.0);
            aggregator.AddValue("test_metric", 10.0);
            aggregator.AddValue("test_metric", 20.0);

            // Act
            var min = aggregator.GetAggregatedValue("test_metric", AggregationType.Min);

            // Assert
            min.Should().Be(10.0);
        }

        [Fact]
        public void GetAggregatedValue_Max_ShouldReturnMaximum()
        {
            // Arrange
            var aggregator = new MetricAggregator();
            aggregator.AddValue("test_metric", 10.0);
            aggregator.AddValue("test_metric", 30.0);
            aggregator.AddValue("test_metric", 20.0);

            // Act
            var max = aggregator.GetAggregatedValue("test_metric", AggregationType.Max);

            // Assert
            max.Should().Be(30.0);
        }

        [Fact]
        public void GetAggregatedValue_Count_ShouldReturnCount()
        {
            // Arrange
            var aggregator = new MetricAggregator();
            aggregator.AddValue("test_metric", 10.0);
            aggregator.AddValue("test_metric", 20.0);
            aggregator.AddValue("test_metric", 30.0);

            // Act
            var count = aggregator.GetAggregatedValue("test_metric", AggregationType.Count);

            // Assert
            count.Should().Be(3.0);
        }

        [Fact]
        public void GetAggregatedValue_Last_ShouldReturnLastValue()
        {
            // Arrange
            var aggregator = new MetricAggregator();
            aggregator.AddValue("test_metric", 10.0);
            aggregator.AddValue("test_metric", 20.0);
            aggregator.AddValue("test_metric", 30.0);

            // Act
            var last = aggregator.GetAggregatedValue("test_metric", AggregationType.Last);

            // Assert
            last.Should().Be(30.0);
        }

        [Fact]
        public void GetAggregatedValue_NonExistentMetric_ShouldReturnNull()
        {
            // Arrange
            var aggregator = new MetricAggregator();

            // Act
            var value = aggregator.GetAggregatedValue("non_existent", AggregationType.Sum);

            // Assert
            value.Should().BeNull();
        }

        [Fact]
        public void GetAggregatedValue_Average_WithNoValues_ShouldReturnNull()
        {
            // Arrange
            var aggregator = new MetricAggregator();
            // Don't add any values

            // Act
            var average = aggregator.GetAggregatedValue("test_metric", AggregationType.Average);

            // Assert
            average.Should().BeNull();
        }

        [Fact]
        public void GetStats_ShouldReturnCompleteStats()
        {
            // Arrange
            var aggregator = new MetricAggregator();
            aggregator.AddValue("test_metric", 10.0);
            aggregator.AddValue("test_metric", 20.0);
            aggregator.AddValue("test_metric", 30.0);

            // Act
            var stats = aggregator.GetStats("test_metric");

            // Assert
            stats.Should().NotBeNull();
            stats!.Name.Should().Be("test_metric");
            stats.Count.Should().Be(3);
            stats.Sum.Should().Be(60.0);
            stats.Average.Should().Be(20.0);
            stats.Min.Should().Be(10.0);
            stats.Max.Should().Be(30.0);
            stats.LastValue.Should().Be(30.0);
            stats.FirstTimestamp.Should().NotBeNull();
            stats.LastTimestamp.Should().NotBeNull();
        }

        [Fact]
        public void GetStats_NonExistentMetric_ShouldReturnNull()
        {
            // Arrange
            var aggregator = new MetricAggregator();

            // Act
            var stats = aggregator.GetStats("non_existent");

            // Assert
            stats.Should().BeNull();
        }

        [Fact]
        public void GetAllStats_ShouldReturnAllMetrics()
        {
            // Arrange
            var aggregator = new MetricAggregator();
            aggregator.AddValue("metric1", 10.0);
            aggregator.AddValue("metric2", 20.0);
            aggregator.AddValue("metric3", 30.0);

            // Act
            var allStats = aggregator.GetAllStats();

            // Assert
            allStats.Should().HaveCount(3);
            allStats.Should().ContainKey("metric1");
            allStats.Should().ContainKey("metric2");
            allStats.Should().ContainKey("metric3");
        }

        [Fact]
        public void Clear_ShouldRemoveAllMetrics()
        {
            // Arrange
            var aggregator = new MetricAggregator();
            aggregator.AddValue("metric1", 10.0);
            aggregator.AddValue("metric2", 20.0);

            // Act
            aggregator.Clear();

            // Assert
            aggregator.GetAllStats().Should().BeEmpty();
            aggregator.GetAggregatedValue("metric1", AggregationType.Sum).Should().BeNull();
        }

        [Fact]
        public void Remove_ShouldRemoveSpecificMetric()
        {
            // Arrange
            var aggregator = new MetricAggregator();
            aggregator.AddValue("metric1", 10.0);
            aggregator.AddValue("metric2", 20.0);

            // Act
            var removed = aggregator.Remove("metric1");

            // Assert
            removed.Should().BeTrue();
            aggregator.GetAggregatedValue("metric1", AggregationType.Sum).Should().BeNull();
            aggregator.GetAggregatedValue("metric2", AggregationType.Sum).Should().Be(20.0);
        }

        [Fact]
        public void Remove_WithTags_ShouldRemoveSpecificMetric()
        {
            // Arrange
            var aggregator = new MetricAggregator();
            var tags = new Dictionary<string, string> { { "env", "test" } };
            aggregator.AddValue("metric1", 10.0, tags);
            aggregator.AddValue("metric1", 20.0); // Without tags

            // Act
            var removed = aggregator.Remove("metric1", tags);

            // Assert
            removed.Should().BeTrue();
            aggregator.GetAggregatedValue("metric1", AggregationType.Sum, tags).Should().BeNull();
            aggregator.GetAggregatedValue("metric1", AggregationType.Sum).Should().Be(20.0);
        }

        [Fact]
        public void Remove_NonExistentMetric_ShouldReturnFalse()
        {
            // Arrange
            var aggregator = new MetricAggregator();

            // Act
            var removed = aggregator.Remove("non_existent");

            // Assert
            removed.Should().BeFalse();
        }
    }
}

