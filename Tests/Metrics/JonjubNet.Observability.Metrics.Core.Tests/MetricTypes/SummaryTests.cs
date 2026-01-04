using FluentAssertions;
using JonjubNet.Observability.Metrics.Core.MetricTypes;
using Xunit;

namespace JonjubNet.Observability.Metrics.Core.Tests.MetricTypes
{
    public class SummaryTests
    {
        [Fact]
        public void Observe_ShouldRecordValue()
        {
            // Arrange
            var summary = new Summary("test_summary", "Test summary description");

            // Act
            summary.Observe(value: 10.5);

            // Assert
            var data = summary.GetData();
            data.Should().NotBeNull();
            data!.Count.Should().Be(1);
            data.Sum.Should().Be(10.5);
        }

        [Fact]
        public void Observe_WithTags_ShouldTrackSeparateSummaries()
        {
            // Arrange
            var summary = new Summary("test_summary", "Test summary description");
            var tags1 = new Dictionary<string, string> { ["env"] = "prod" };
            var tags2 = new Dictionary<string, string> { ["env"] = "dev" };

            // Act
            summary.Observe(tags1, 10.0);
            summary.Observe(tags2, 20.0);

            // Assert
            summary.GetData(tags1)!.Sum.Should().Be(10.0);
            summary.GetData(tags2)!.Sum.Should().Be(20.0);
        }

        [Fact]
        public void Observe_MultipleValues_ShouldAccumulate()
        {
            // Arrange
            var summary = new Summary("test_summary", "Test summary description");

            // Act
            summary.Observe(value: 5.0);
            summary.Observe(value: 10.0);
            summary.Observe(value: 15.0);

            // Assert
            var data = summary.GetData();
            data.Should().NotBeNull();
            data!.Count.Should().Be(3);
            data.Sum.Should().Be(30.0);
        }

        [Fact]
        public void Quantiles_ShouldUseDefaultQuantiles()
        {
            // Arrange
            var summary = new Summary("test_summary", "Test summary description");

            // Assert
            summary.Quantiles.Should().NotBeEmpty();
            summary.Quantiles.Should().Contain(0.5);
            summary.Quantiles.Should().Contain(0.95);
            summary.Quantiles.Should().Contain(0.99);
        }

        [Fact]
        public void Quantiles_ShouldUseCustomQuantiles()
        {
            // Arrange
            var customQuantiles = new double[] { 0.5, 0.9 };
            var summary = new Summary("test_summary", "Test summary description", customQuantiles);

            // Assert
            summary.Quantiles.Should().Equal(customQuantiles);
        }
    }
}

