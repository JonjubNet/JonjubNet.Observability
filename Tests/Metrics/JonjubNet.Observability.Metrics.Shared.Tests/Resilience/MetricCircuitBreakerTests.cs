using FluentAssertions;
using JonjubNet.Observability.Metrics.Core.Resilience;
using JonjubNet.Observability.Metrics.Shared.Resilience;
using Xunit;

namespace JonjubNet.Observability.Metrics.Shared.Tests.Resilience
{
    public class MetricCircuitBreakerTests
    {
        [Fact]
        public async Task ExecuteAsync_Success_ShouldReturnResult()
        {
            // Arrange
            var circuitBreaker = new MetricCircuitBreaker(failureThreshold: 3);

            // Act
            var result = await circuitBreaker.ExecuteAsync(() => Task.FromResult(42));

            // Assert
            result.Should().Be(42);
            circuitBreaker.State.Should().Be(CircuitState.Closed);
        }

        [Fact]
        public async Task ExecuteAsync_FailuresBelowThreshold_ShouldNotOpen()
        {
            // Arrange
            var circuitBreaker = new MetricCircuitBreaker(failureThreshold: 3);
            var callCount = 0;

            // Act
            try
            {
                await circuitBreaker.ExecuteAsync(() =>
                {
                    callCount++;
                    if (callCount < 3)
                        throw new Exception("Test error");
                    return Task.FromResult(42);
                });
            }
            catch { }

            // Assert
            circuitBreaker.State.Should().Be(CircuitState.Closed);
        }

        [Fact]
        public async Task ExecuteAsync_FailuresAboveThreshold_ShouldOpen()
        {
            // Arrange
            var circuitBreaker = new MetricCircuitBreaker(failureThreshold: 2);

            // Act
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    await circuitBreaker.ExecuteAsync(() => throw new Exception("Test error"));
                }
                catch { }
            }

            // Assert
            circuitBreaker.State.Should().Be(CircuitState.Open);
        }

        [Fact]
        public async Task ExecuteAsync_OpenCircuit_ShouldThrow()
        {
            // Arrange
            var circuitBreaker = new MetricCircuitBreaker(failureThreshold: 1, openDuration: TimeSpan.FromSeconds(1));

            // Act - Open the circuit
            try
            {
                await circuitBreaker.ExecuteAsync(() => throw new Exception("Test error"));
            }
            catch { }

            // Assert - Should throw CircuitBreakerOpenException
            await Assert.ThrowsAsync<CircuitBreakerOpenException>(async () =>
            {
                await circuitBreaker.ExecuteAsync(() => Task.FromResult(42));
            });
        }
    }
}

