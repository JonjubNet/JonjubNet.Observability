using FluentAssertions;
using JonjubNet.Observability.Tracing.Core.Resilience;
using Xunit;

namespace JonjubNet.Observability.Tracing.Core.Tests.Resilience
{
    public class RetryPolicyTests
    {
        [Fact]
        public async Task ExecuteAsync_ShouldSucceedOnFirstAttempt()
        {
            // Arrange
            var policy = new RetryPolicy(maxRetries: 3);
            var attemptCount = 0;

            // Act
            var result = await policy.ExecuteAsync(async () =>
            {
                attemptCount++;
                return await Task.FromResult(42);
            });

            // Assert
            result.Should().Be(42);
            attemptCount.Should().Be(1);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldRetryOnFailure()
        {
            // Arrange
            var policy = new RetryPolicy(maxRetries: 2, initialDelay: TimeSpan.FromMilliseconds(10));
            var attemptCount = 0;

            // Act
            var result = await policy.ExecuteAsync(async () =>
            {
                attemptCount++;
                if (attemptCount < 2)
                    throw new InvalidOperationException("Retry");
                return await Task.FromResult(42);
            });

            // Assert
            result.Should().Be(42);
            attemptCount.Should().Be(2);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldThrowAfterMaxRetries()
        {
            // Arrange
            var policy = new RetryPolicy(maxRetries: 2, initialDelay: TimeSpan.FromMilliseconds(10));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await policy.ExecuteAsync(async () =>
                {
                    throw new InvalidOperationException("Always fails");
                });
            });
        }

        [Fact]
        public async Task ExecuteWithResultAsync_ShouldReturnSuccessResult()
        {
            // Arrange
            var policy = new RetryPolicy(maxRetries: 3, initialDelay: TimeSpan.FromMilliseconds(10));

            // Act
            var result = await policy.ExecuteWithResultAsync(async () =>
            {
                return await Task.FromResult("Success");
            });

            // Assert
            result.Success.Should().BeTrue();
            result.Value.Should().Be("Success");
            result.TotalAttempts.Should().Be(1);
            result.Attempts.Should().HaveCount(1);
        }

        [Fact]
        public async Task ExecuteWithResultAsync_ShouldReturnFailureResultAfterMaxRetries()
        {
            // Arrange
            var policy = new RetryPolicy(maxRetries: 2, initialDelay: TimeSpan.FromMilliseconds(10));

            // Act
            var result = await policy.ExecuteWithResultAsync<string>(async () =>
            {
                throw new InvalidOperationException("Always fails");
            });

            // Assert
            result.Success.Should().BeFalse();
            result.TotalAttempts.Should().Be(3); // Initial + 2 retries
            result.LastException.Should().NotBeNull();
            result.Attempts.Should().HaveCount(3);
            result.Attempts.All(a => !a.Success).Should().BeTrue();
        }
    }
}

