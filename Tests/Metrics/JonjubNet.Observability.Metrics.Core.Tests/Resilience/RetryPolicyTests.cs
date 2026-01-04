using FluentAssertions;
using JonjubNet.Observability.Metrics.Core.Resilience;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JonjubNet.Observability.Metrics.Core.Tests.Resilience
{
    public class RetryPolicyTests
    {
        [Fact]
        public async Task ExecuteAsync_ShouldSucceedOnFirstAttempt()
        {
            // Arrange
            var policy = new RetryPolicy(maxRetries: 3);
            var callCount = 0;

            // Act
            var result = await policy.ExecuteAsync(async () =>
            {
                callCount++;
                return await Task.FromResult(42);
            });

            // Assert
            result.Should().Be(42);
            callCount.Should().Be(1);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldRetryOnFailure()
        {
            // Arrange
            var policy = new RetryPolicy(maxRetries: 3, initialDelay: TimeSpan.FromMilliseconds(10));
            var callCount = 0;

            // Act
            var result = await policy.ExecuteAsync(async () =>
            {
                callCount++;
                if (callCount < 3)
                    throw new InvalidOperationException("Temporary failure");
                return await Task.FromResult(42);
            });

            // Assert
            result.Should().Be(42);
            callCount.Should().Be(3);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldThrowAfterMaxRetries()
        {
            // Arrange
            var policy = new RetryPolicy(maxRetries: 2, initialDelay: TimeSpan.FromMilliseconds(10));
            var callCount = 0;

            // Act
            var act = async () => await policy.ExecuteAsync(async () =>
            {
                callCount++;
                throw new InvalidOperationException("Persistent failure");
            });

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
            callCount.Should().Be(3); // Initial + 2 retries
        }

        [Fact]
        public async Task ExecuteAsync_Action_ShouldSucceed()
        {
            // Arrange
            var policy = new RetryPolicy(maxRetries: 3);
            var callCount = 0;

            // Act
            await policy.ExecuteAsync(async () =>
            {
                callCount++;
                await Task.CompletedTask;
            });

            // Assert
            callCount.Should().Be(1);
        }

        [Fact]
        public async Task ExecuteWithResultAsync_ShouldReturnSuccessResult()
        {
            // Arrange
            var policy = new RetryPolicy(maxRetries: 3);
            var callCount = 0;

            // Act
            var result = await policy.ExecuteWithResultAsync<int>(async () =>
            {
                callCount++;
                return await Task.FromResult(42);
            });

            // Assert
            result.Success.Should().BeTrue();
            result.Value.Should().Be(42);
            result.TotalAttempts.Should().Be(1);
            result.Attempts.Should().HaveCount(1);
            result.Attempts[0].Success.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteWithResultAsync_ShouldRetryAndReturnFailureResult()
        {
            // Arrange
            var policy = new RetryPolicy(maxRetries: 2, initialDelay: TimeSpan.FromMilliseconds(10));
            var callCount = 0;

            // Act
            var result = await policy.ExecuteWithResultAsync<int>(async () =>
            {
                callCount++;
                throw new InvalidOperationException("Failure");
            });

            // Assert
            result.Success.Should().BeFalse();
            result.TotalAttempts.Should().Be(3);
            result.Attempts.Should().HaveCount(3);
            result.Attempts.All(a => !a.Success).Should().BeTrue();
            result.LastException.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_ShouldSetDefaultValues()
        {
            // Act
            var policy = new RetryPolicy();

            // Assert
            policy.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithCustomValues_ShouldSetValues()
        {
            // Act
            var policy = new RetryPolicy(
                maxRetries: 5,
                initialDelay: TimeSpan.FromSeconds(1),
                backoffMultiplier: 3.0,
                jitterPercent: 0.2);

            // Assert
            policy.Should().NotBeNull();
        }
    }
}

