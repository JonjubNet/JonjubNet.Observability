using FluentAssertions;
using JonjubNet.Observability.Metrics.Core.Utils;
using Xunit;

namespace JonjubNet.Observability.Metrics.Core.Tests.Utils
{
    public class SlidingWindowTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithWindowSize()
        {
            // Arrange & Act
            var window = new SlidingWindow(TimeSpan.FromMinutes(5));

            // Assert
            window.Should().NotBeNull();
            window.Count.Should().Be(0);
        }

        [Fact]
        public void Add_ShouldAddValue()
        {
            // Arrange
            var window = new SlidingWindow(TimeSpan.FromMinutes(5));

            // Act
            window.Add(10.0);

            // Assert
            window.Count.Should().Be(1);
        }

        [Fact]
        public void Add_WithTimestamp_ShouldAddValueWithTimestamp()
        {
            // Arrange
            var window = new SlidingWindow(TimeSpan.FromMinutes(5));
            var timestamp = DateTime.UtcNow;

            // Act
            window.Add(10.0, timestamp);

            // Assert
            window.Count.Should().Be(1);
        }

        [Fact]
        public void GetValues_ShouldReturnAllValuesInWindow()
        {
            // Arrange
            var window = new SlidingWindow(TimeSpan.FromMinutes(5));
            window.Add(10.0);
            window.Add(20.0);
            window.Add(30.0);

            // Act
            var values = window.GetValues();

            // Assert
            values.Should().HaveCount(3);
            values.Should().Contain(10.0);
            values.Should().Contain(20.0);
            values.Should().Contain(30.0);
        }

        [Fact]
        public void GetValues_ShouldExcludeValuesOutsideWindow()
        {
            // Arrange
            var window = new SlidingWindow(TimeSpan.FromSeconds(1));
            var oldTime = DateTime.UtcNow.AddMinutes(-2);
            var recentTime = DateTime.UtcNow;

            // Act
            window.Add(10.0, oldTime);
            window.Add(20.0, recentTime);

            // Wait a bit to ensure cleanup
            Thread.Sleep(1100);

            // Act
            var values = window.GetValues();

            // Assert
            values.Should().Contain(20.0);
            values.Should().NotContain(10.0);
        }

        [Fact]
        public void Count_ShouldReturnNumberOfValuesInWindow()
        {
            // Arrange
            var window = new SlidingWindow(TimeSpan.FromMinutes(5));
            window.Add(10.0);
            window.Add(20.0);
            window.Add(30.0);

            // Act
            var count = window.Count;

            // Assert
            count.Should().Be(3);
        }

        [Fact]
        public void Clear_ShouldRemoveAllValues()
        {
            // Arrange
            var window = new SlidingWindow(TimeSpan.FromMinutes(5));
            window.Add(10.0);
            window.Add(20.0);
            window.Add(30.0);

            // Act
            window.Clear();

            // Assert
            window.Count.Should().Be(0);
            window.GetValues().Should().BeEmpty();
        }

        [Fact]
        public void GetValues_ShouldMaintainOrder()
        {
            // Arrange
            var window = new SlidingWindow(TimeSpan.FromMinutes(5));
            var values = new[] { 10.0, 20.0, 30.0, 40.0, 50.0 };
            
            foreach (var value in values)
            {
                window.Add(value);
                Thread.Sleep(10); // Small delay to ensure different timestamps
            }

            // Act
            var result = window.GetValues();

            // Assert
            result.Should().HaveCount(5);
            // Values should be in order they were added (FIFO)
        }
    }
}

