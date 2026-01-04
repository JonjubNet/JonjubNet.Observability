using FluentAssertions;
using JonjubNet.Observability.Metrics.Core.MetricTypes;
using Xunit;

namespace JonjubNet.Observability.Metrics.Core.Tests.MetricTypes
{
    public class CounterTests
    {
        [Fact]
        public void Inc_WithoutTags_ShouldIncrementByOne()
        {
            // Arrange
            var counter = new Counter("test_counter", "Test counter description");

            // Act
            counter.Inc();

            // Assert
            counter.GetValue().Should().Be(1);
        }

        [Fact]
        public void Inc_WithCustomValue_ShouldIncrementByValue()
        {
            // Arrange
            var counter = new Counter("test_counter", "Test counter description");

            // Act
            counter.Inc(value: 5.0);

            // Assert
            counter.GetValue().Should().Be(5);
        }

        [Fact]
        public void Inc_WithTags_ShouldTrackSeparateCounters()
        {
            // Arrange
            var counter = new Counter("test_counter", "Test counter description");
            var tags1 = new Dictionary<string, string> { ["env"] = "prod" };
            var tags2 = new Dictionary<string, string> { ["env"] = "dev" };

            // Act
            counter.Inc(tags1, 2.0);
            counter.Inc(tags2, 3.0);

            // Assert
            counter.GetValue(tags1).Should().Be(2);
            counter.GetValue(tags2).Should().Be(3);
        }

        [Fact]
        public void Inc_MultipleTimes_ShouldAccumulate()
        {
            // Arrange
            var counter = new Counter("test_counter", "Test counter description");

            // Act
            counter.Inc(value: 2.0);
            counter.Inc(value: 3.0);
            counter.Inc(value: 5.0);

            // Assert
            counter.GetValue().Should().Be(10);
        }

        [Fact]
        public void GetValue_WithoutIncrement_ShouldReturnZero()
        {
            // Arrange
            var counter = new Counter("test_counter", "Test counter description");

            // Act
            var value = counter.GetValue();

            // Assert
            value.Should().Be(0);
        }

        [Fact]
        public void GetAllValues_ShouldReturnAllTaggedCounters()
        {
            // Arrange
            var counter = new Counter("test_counter", "Test counter description");
            var tags1 = new Dictionary<string, string> { ["env"] = "prod" };
            var tags2 = new Dictionary<string, string> { ["env"] = "dev" };

            // Act
            counter.Inc(tags1, 10.0);
            counter.Inc(tags2, 20.0);
            var allValues = counter.GetAllValues();

            // Assert
            allValues.Should().HaveCount(2);
            allValues.Values.Should().Contain(10);
            allValues.Values.Should().Contain(20);
        }

        [Fact]
        public void Name_ShouldReturnCorrectName()
        {
            // Arrange
            var counter = new Counter("my_counter", "Description");

            // Assert
            counter.Name.Should().Be("my_counter");
        }

        [Fact]
        public void Description_ShouldReturnCorrectDescription()
        {
            // Arrange
            var counter = new Counter("my_counter", "My description");

            // Assert
            counter.Description.Should().Be("My description");
        }

        [Fact]
        public void Inc_WithSameTags_ShouldAccumulate()
        {
            // Arrange
            var counter = new Counter("test_counter", "Test counter description");
            var tags = new Dictionary<string, string> { ["env"] = "prod" };

            // Act
            counter.Inc(tags, 5.0);
            counter.Inc(tags, 3.0);

            // Assert
            counter.GetValue(tags).Should().Be(8);
        }
    }
}

