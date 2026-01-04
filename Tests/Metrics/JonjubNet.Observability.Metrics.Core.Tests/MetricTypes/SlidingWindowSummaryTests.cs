using FluentAssertions;
using JonjubNet.Observability.Metrics.Core.MetricTypes;
using Xunit;

namespace JonjubNet.Observability.Metrics.Core.Tests.MetricTypes
{
    public class SlidingWindowSummaryTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithProperties()
        {
            // Arrange & Act
            var windowSize = TimeSpan.FromMinutes(5);
            var quantiles = new double[] { 0.5, 0.95, 0.99 };
            var summary = new SlidingWindowSummary("test_summary", "Description", windowSize, quantiles);

            // Assert
            summary.Name.Should().Be("test_summary");
            summary.Description.Should().Be("Description");
            summary.WindowSize.Should().Be(windowSize);
            summary.Quantiles.Should().BeEquivalentTo(quantiles);
        }

        [Fact]
        public void Constructor_WithoutQuantiles_ShouldUseDefaultQuantiles()
        {
            // Arrange & Act
            var summary = new SlidingWindowSummary("test_summary", "Description", TimeSpan.FromMinutes(5));

            // Assert
            summary.Quantiles.Should().NotBeEmpty();
            summary.Quantiles.Should().Contain(0.5);
            summary.Quantiles.Should().Contain(0.95);
            summary.Quantiles.Should().Contain(0.99);
            summary.Quantiles.Should().Contain(0.999);
        }

        [Fact]
        public void Observe_ShouldAddValue()
        {
            // Arrange
            var summary = new SlidingWindowSummary("test_summary", "Description", TimeSpan.FromMinutes(5));

            // Act
            summary.Observe(value: 10.0);

            // Assert
            var data = summary.GetData();
            data.Should().NotBeNull();
        }

        [Fact]
        public void Observe_WithTags_ShouldCreateSeparateData()
        {
            // Arrange
            var summary = new SlidingWindowSummary("test_summary", "Description", TimeSpan.FromMinutes(5));
            var tags1 = new Dictionary<string, string> { { "env", "test" } };
            var tags2 = new Dictionary<string, string> { { "env", "prod" } };

            // Act
            summary.Observe(tags1, 10.0);
            summary.Observe(tags2, 20.0);

            // Assert
            var data1 = summary.GetData(tags1);
            var data2 = summary.GetData(tags2);
            data1.Should().NotBeNull();
            data2.Should().NotBeNull();
            data1.Should().NotBeSameAs(data2);
        }

        [Fact]
        public void GetData_WithoutObserving_ShouldReturnNull()
        {
            // Arrange
            var summary = new SlidingWindowSummary("test_summary", "Description", TimeSpan.FromMinutes(5));

            // Act
            var data = summary.GetData();

            // Assert
            data.Should().BeNull();
        }

        [Fact]
        public void GetAllData_ShouldReturnAllData()
        {
            // Arrange
            var summary = new SlidingWindowSummary("test_summary", "Description", TimeSpan.FromMinutes(5));
            var tags1 = new Dictionary<string, string> { { "env", "test" } };
            var tags2 = new Dictionary<string, string> { { "env", "prod" } };

            // Act
            summary.Observe(tags1, 10.0);
            summary.Observe(tags2, 20.0);

            // Assert
            var allData = summary.GetAllData();
            allData.Should().NotBeEmpty();
        }
    }
}

