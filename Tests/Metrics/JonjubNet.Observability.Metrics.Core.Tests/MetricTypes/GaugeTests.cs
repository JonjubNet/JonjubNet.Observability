using FluentAssertions;
using JonjubNet.Observability.Metrics.Core.MetricTypes;
using Xunit;

namespace JonjubNet.Observability.Metrics.Core.Tests.MetricTypes
{
    public class GaugeTests
    {
        [Fact]
        public void Set_ShouldSetValue()
        {
            // Arrange
            var gauge = new Gauge("test_gauge", "Test gauge description");

            // Act
            gauge.Set(value: 42.5);

            // Assert
            gauge.GetValue().Should().Be(42.5);
        }

        [Fact]
        public void Set_WithTags_ShouldTrackSeparateGauges()
        {
            // Arrange
            var gauge = new Gauge("test_gauge", "Test gauge description");
            var tags1 = new Dictionary<string, string> { ["env"] = "prod" };
            var tags2 = new Dictionary<string, string> { ["env"] = "dev" };

            // Act
            gauge.Set(tags1, 100.0);
            gauge.Set(tags2, 200.0);

            // Assert
            gauge.GetValue(tags1).Should().Be(100.0);
            gauge.GetValue(tags2).Should().Be(200.0);
        }

        [Fact]
        public void Inc_ShouldIncrementValue()
        {
            // Arrange
            var gauge = new Gauge("test_gauge", "Test gauge description");

            // Act
            gauge.Set(value: 10.0);
            gauge.Inc(value: 5.0);

            // Assert
            gauge.GetValue().Should().Be(15.0);
        }

        [Fact]
        public void Dec_ShouldDecrementValue()
        {
            // Arrange
            var gauge = new Gauge("test_gauge", "Test gauge description");

            // Act
            gauge.Set(value: 10.0);
            gauge.Dec(value: 3.0);

            // Assert
            gauge.GetValue().Should().Be(7.0);
        }

        [Fact]
        public void Inc_WithoutInitialValue_ShouldStartFromIncrement()
        {
            // Arrange
            var gauge = new Gauge("test_gauge", "Test gauge description");

            // Act
            gauge.Inc(value: 5.0);

            // Assert
            gauge.GetValue().Should().Be(5.0);
        }

        [Fact]
        public void Dec_WithoutInitialValue_ShouldStartFromNegative()
        {
            // Arrange
            var gauge = new Gauge("test_gauge", "Test gauge description");

            // Act
            gauge.Dec(value: 5.0);

            // Assert
            gauge.GetValue().Should().Be(-5.0);
        }

        [Fact]
        public void Set_OverwritesPreviousValue()
        {
            // Arrange
            var gauge = new Gauge("test_gauge", "Test gauge description");

            // Act
            gauge.Set(value: 10.0);
            gauge.Set(value: 20.0);

            // Assert
            gauge.GetValue().Should().Be(20.0);
        }

        [Fact]
        public void GetAllValues_ShouldReturnAllTaggedGauges()
        {
            // Arrange
            var gauge = new Gauge("test_gauge", "Test gauge description");
            var tags1 = new Dictionary<string, string> { ["env"] = "prod" };
            var tags2 = new Dictionary<string, string> { ["env"] = "dev" };

            // Act
            gauge.Set(tags1, 100.0);
            gauge.Set(tags2, 200.0);
            var allValues = gauge.GetAllValues();

            // Assert
            allValues.Should().HaveCount(2);
            allValues.Values.Should().Contain(100.0);
            allValues.Values.Should().Contain(200.0);
        }

        [Fact]
        public void Name_ShouldReturnCorrectName()
        {
            // Arrange
            var gauge = new Gauge("my_gauge", "Description");

            // Assert
            gauge.Name.Should().Be("my_gauge");
        }

        [Fact]
        public void Description_ShouldReturnCorrectDescription()
        {
            // Arrange
            var gauge = new Gauge("my_gauge", "My description");

            // Assert
            gauge.Description.Should().Be("My description");
        }
    }
}

