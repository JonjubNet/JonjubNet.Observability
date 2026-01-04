using FluentAssertions;
using JonjubNet.Observability.Metrics.Core.MetricTypes;
using Xunit;

namespace JonjubNet.Observability.Metrics.Core.Tests.MetricTypes
{
    public class HistogramTests
    {
        [Fact]
        public void Observe_ShouldRecordValue()
        {
            // Arrange
            var histogram = new Histogram("test_histogram", "Test histogram description");

            // Act
            histogram.Observe(value: 10.5);

            // Assert
            var data = histogram.GetData();
            data.Should().NotBeNull();
            data!.Count.Should().Be(1);
            data.Sum.Should().Be(10.5);
        }

        [Fact]
        public void Observe_WithTags_ShouldTrackSeparateHistograms()
        {
            // Arrange
            var histogram = new Histogram("test_histogram", "Test histogram description");
            var tags1 = new Dictionary<string, string> { ["env"] = "prod" };
            var tags2 = new Dictionary<string, string> { ["env"] = "dev" };

            // Act
            histogram.Observe(tags1, 10.0);
            histogram.Observe(tags2, 20.0);

            // Assert
            histogram.GetData(tags1)!.Sum.Should().Be(10.0);
            histogram.GetData(tags2)!.Sum.Should().Be(20.0);
        }

        [Fact]
        public void Observe_MultipleValues_ShouldAccumulate()
        {
            // Arrange
            var histogram = new Histogram("test_histogram", "Test histogram description");

            // Act
            histogram.Observe(value: 5.0);
            histogram.Observe(value: 10.0);
            histogram.Observe(value: 15.0);

            // Assert
            var data = histogram.GetData();
            data.Should().NotBeNull();
            data!.Count.Should().Be(3);
            data.Sum.Should().Be(30.0);
        }

        [Fact]
        public void GetData_WithoutObserve_ShouldReturnNull()
        {
            // Arrange
            var histogram = new Histogram("test_histogram", "Test histogram description");

            // Act
            var data = histogram.GetData();

            // Assert
            data.Should().BeNull();
        }

        [Fact]
        public void Buckets_ShouldUseDefaultBuckets()
        {
            // Arrange
            var histogram = new Histogram("test_histogram", "Test histogram description");

            // Assert
            histogram.Buckets.Should().NotBeEmpty();
            histogram.Buckets.Should().BeInAscendingOrder();
        }

        [Fact]
        public void Buckets_ShouldUseCustomBuckets()
        {
            // Arrange
            var customBuckets = new double[] { 0.1, 0.5, 1.0, 2.5, 5.0 };
            var histogram = new Histogram("test_histogram", "Test histogram description", customBuckets);

            // Assert
            histogram.Buckets.Should().Equal(customBuckets);
        }
    }
}

