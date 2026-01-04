using FluentAssertions;
using JonjubNet.Observability.Metrics.Core;
using Xunit;

namespace JonjubNet.Observability.Metrics.Core.Tests
{
    public class MetricPointTests
    {
        [Fact]
        public void Constructor_ShouldCreateMetricPointWithAllProperties()
        {
            // Arrange & Act
            var point = new MetricPoint("test_metric", MetricType.Counter, 42.5);

            // Assert
            point.Name.Should().Be("test_metric");
            point.Type.Should().Be(MetricType.Counter);
            point.Value.Should().Be(42.5);
            point.Tags.Should().NotBeNull();
            point.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Constructor_WithTags_ShouldSetTags()
        {
            // Arrange
            var tags = new Dictionary<string, string> { { "env", "test" }, { "service", "api" } };

            // Act
            var point = new MetricPoint("test_metric", MetricType.Gauge, 10.0, tags);

            // Assert
            point.Tags.Should().HaveCount(2);
            point.Tags.Should().ContainKey("env").WhoseValue.Should().Be("test");
            point.Tags.Should().ContainKey("service").WhoseValue.Should().Be("api");
        }

        [Fact]
        public void Constructor_WithoutTags_ShouldUseEmptyTags()
        {
            // Act
            var point1 = new MetricPoint("test1", MetricType.Counter, 1.0);
            var point2 = new MetricPoint("test2", MetricType.Gauge, 2.0);

            // Assert - Both should use the same empty tags singleton
            point1.Tags.Should().BeEmpty();
            point2.Tags.Should().BeEmpty();
            point1.Tags.Should().BeSameAs(point2.Tags);
        }

        [Fact]
        public void Constructor_WithNullTags_ShouldUseEmptyTags()
        {
            // Act
            var point = new MetricPoint("test_metric", MetricType.Histogram, 5.0, null);

            // Assert
            point.Tags.Should().NotBeNull();
            point.Tags.Should().BeEmpty();
        }

        [Theory]
        [InlineData(MetricType.Counter)]
        [InlineData(MetricType.Gauge)]
        [InlineData(MetricType.Histogram)]
        [InlineData(MetricType.Summary)]
        [InlineData(MetricType.Timer)]
        public void Constructor_ShouldSupportAllMetricTypes(MetricType type)
        {
            // Act
            var point = new MetricPoint("test", type, 1.0);

            // Assert
            point.Type.Should().Be(type);
        }

        [Fact]
        public void MetricPoint_ShouldBeRecordStruct()
        {
            // Arrange
            var point1 = new MetricPoint("test", MetricType.Counter, 1.0);
            var point2 = new MetricPoint("test", MetricType.Counter, 1.0);

            // Act & Assert - Record structs should support equality
            point1.Should().Be(point2);
        }
    }
}

