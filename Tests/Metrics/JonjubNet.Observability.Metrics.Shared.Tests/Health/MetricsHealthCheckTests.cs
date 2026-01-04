using FluentAssertions;
using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.Core.Interfaces;
using JonjubNet.Observability.Metrics.Shared.Health;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JonjubNet.Observability.Metrics.Shared.Tests.Health
{
    public class MetricsHealthCheckTests
    {
        [Fact]
        public void CheckSinksHealth_WithNoSinks_ShouldReturnEmpty()
        {
            // Arrange
            var healthCheck = new MetricsHealthCheck();

            // Act
            var result = healthCheck.CheckSinksHealth();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void CheckSchedulerHealth_WithNoScheduler_ShouldReturnNotRunning()
        {
            // Arrange
            var healthCheck = new MetricsHealthCheck();

            // Act
            var result = healthCheck.CheckSchedulerHealth();

            // Assert
            result.IsRunning.Should().BeFalse();
            result.IsHealthy.Should().BeTrue();
        }

        [Fact]
        public void GetOverallHealth_WithNoComponents_ShouldReturnHealthy()
        {
            // Arrange
            var healthCheck = new MetricsHealthCheck();

            // Act
            var result = healthCheck.GetOverallHealth();

            // Assert
            result.IsHealthy.Should().BeTrue();
        }

        [Fact]
        public void CheckSinksHealth_WithSink_ShouldReturnStatus()
        {
            // Arrange
            var mockSink = new Mock<IMetricsSink>();
            mockSink.Setup(s => s.Name).Returns("TestSink");
            mockSink.Setup(s => s.IsEnabled).Returns(true);
            var sinks = new[] { mockSink.Object };
            var healthCheck = new MetricsHealthCheck(sinks: sinks);

            // Act
            var result = healthCheck.CheckSinksHealth();

            // Assert
            result.Should().ContainKey("TestSink");
            result["TestSink"].IsEnabled.Should().BeTrue();
        }
    }
}

