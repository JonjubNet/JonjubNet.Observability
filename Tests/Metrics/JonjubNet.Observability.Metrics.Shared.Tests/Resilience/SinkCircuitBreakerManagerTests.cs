using FluentAssertions;
using JonjubNet.Observability.Metrics.Core.Interfaces;
using JonjubNet.Observability.Metrics.Core.Resilience;
using JonjubNet.Observability.Metrics.Shared.Resilience;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JonjubNet.Observability.Metrics.Shared.Tests.Resilience
{
    public class SinkCircuitBreakerManagerTests
    {
        [Fact]
        public void GetOrCreateCircuitBreaker_WithEnabled_ShouldReturnCircuitBreaker()
        {
            // Arrange
            var mockSink = new Mock<IMetricsSink>();
            mockSink.Setup(s => s.Name).Returns("TestSink");
            var manager = new SinkCircuitBreakerManager(enabled: true);

            // Act
            var circuitBreaker = manager.GetOrCreateCircuitBreaker(mockSink.Object);

            // Assert
            circuitBreaker.Should().NotBeNull();
        }

        [Fact]
        public void GetOrCreateCircuitBreaker_WithDisabled_ShouldReturnNull()
        {
            // Arrange
            var mockSink = new Mock<IMetricsSink>();
            mockSink.Setup(s => s.Name).Returns("TestSink");
            var manager = new SinkCircuitBreakerManager(enabled: false);

            // Act
            var circuitBreaker = manager.GetOrCreateCircuitBreaker(mockSink.Object);

            // Assert
            circuitBreaker.Should().BeNull();
        }

        [Fact]
        public void GetSinkCircuitState_WithExistingSink_ShouldReturnState()
        {
            // Arrange
            var mockSink = new Mock<IMetricsSink>();
            mockSink.Setup(s => s.Name).Returns("TestSink");
            var manager = new SinkCircuitBreakerManager(enabled: true);
            manager.GetOrCreateCircuitBreaker(mockSink.Object);

            // Act
            var state = manager.GetSinkCircuitState("TestSink");

            // Assert
            state.Should().NotBeNull();
        }

        [Fact]
        public void GetAllCircuitStates_ShouldReturnAllStates()
        {
            // Arrange
            var mockSink1 = new Mock<IMetricsSink>();
            mockSink1.Setup(s => s.Name).Returns("Sink1");
            var mockSink2 = new Mock<IMetricsSink>();
            mockSink2.Setup(s => s.Name).Returns("Sink2");
            var manager = new SinkCircuitBreakerManager(enabled: true);
            manager.GetOrCreateCircuitBreaker(mockSink1.Object);
            manager.GetOrCreateCircuitBreaker(mockSink2.Object);

            // Act
            var states = manager.GetAllCircuitStates();

            // Assert
            states.Should().ContainKey("Sink1");
            states.Should().ContainKey("Sink2");
        }
    }
}

