using FluentAssertions;
using JonjubNet.Observability.Metrics.Shared.Security;
using Xunit;

namespace JonjubNet.Observability.Metrics.Shared.Tests.Security
{
    public class SecureTagValidatorTests
    {
        [Fact]
        public void ValidateAndSanitize_ValidTags_ShouldReturnSame()
        {
            // Arrange
            var validator = new SecureTagValidator();
            var tags = new Dictionary<string, string>
            {
                ["env"] = "prod",
                ["service"] = "api"
            };

            // Act
            var result = validator.ValidateAndSanitize(tags);

            // Assert
            result.Should().HaveCount(2);
            result["env"].Should().Be("prod");
            result["service"].Should().Be("api");
        }

        [Fact]
        public void ValidateAndSanitize_BlacklistedKey_ShouldRemove()
        {
            // Arrange
            var validator = new SecureTagValidator();
            var tags = new Dictionary<string, string>
            {
                ["password"] = "secret123",
                ["env"] = "prod"
            };

            // Act
            var result = validator.ValidateAndSanitize(tags);

            // Assert
            result.Should().HaveCount(1);
            result.Should().NotContainKey("password");
            result.Should().ContainKey("env");
        }

        [Fact]
        public void ValidateAndSanitize_InvalidKeyFormat_ShouldRemove()
        {
            // Arrange
            var validator = new SecureTagValidator();
            var tags = new Dictionary<string, string>
            {
                ["invalid-key"] = "value",
                ["valid_key"] = "value"
            };

            // Act
            var result = validator.ValidateAndSanitize(tags);

            // Assert
            result.Should().HaveCount(1);
            result.Should().NotContainKey("invalid-key");
            result.Should().ContainKey("valid_key");
        }

        [Fact]
        public void IsKeySafe_ValidKey_ShouldReturnTrue()
        {
            // Arrange
            var validator = new SecureTagValidator();

            // Act
            var result = validator.IsKeySafe("valid_key_123");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsKeySafe_BlacklistedKey_ShouldReturnFalse()
        {
            // Arrange
            var validator = new SecureTagValidator();

            // Act
            var result = validator.IsKeySafe("password");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValueSafe_Email_ShouldReturnFalse()
        {
            // Arrange
            var validator = new SecureTagValidator();

            // Act
            var result = validator.IsValueSafe("user@example.com");

            // Assert
            result.Should().BeFalse();
        }
    }
}

