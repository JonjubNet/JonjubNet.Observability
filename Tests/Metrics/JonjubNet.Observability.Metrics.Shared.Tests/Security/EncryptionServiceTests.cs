using FluentAssertions;
using JonjubNet.Observability.Shared.Security;
using Xunit;

namespace JonjubNet.Observability.Metrics.Shared.Tests.Security
{
    public class EncryptionServiceTests
    {
        [Fact]
        public void EncryptString_AndDecryptString_ShouldRoundTrip()
        {
            // Arrange
            var encryption = new EncryptionService();
            var plainText = "sensitive-metric-data";

            // Act
            var encrypted = encryption.EncryptString(plainText);
            var decrypted = encryption.DecryptString(encrypted);

            // Assert
            encrypted.Should().NotBe(plainText);
            decrypted.Should().Be(plainText);
        }

        [Fact]
        public void Encrypt_AndDecrypt_ShouldRoundTrip()
        {
            // Arrange
            var encryption = new EncryptionService();
            var plainBytes = System.Text.Encoding.UTF8.GetBytes("test-data");

            // Act
            var encrypted = encryption.Encrypt(plainBytes);
            var decrypted = encryption.Decrypt(encrypted);

            // Assert
            encrypted.Should().NotEqual(plainBytes);
            decrypted.Should().Equal(plainBytes);
        }

        [Fact]
        public void EncryptString_WithCustomKey_ShouldWork()
        {
            // Arrange
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.GenerateKey();
            aes.GenerateIV();
            
            var encryption = new EncryptionService(aes.Key, aes.IV);
            var plainText = "test-data";

            // Act
            var encrypted = encryption.EncryptString(plainText);
            var decrypted = encryption.DecryptString(encrypted);

            // Assert
            decrypted.Should().Be(plainText);
        }

        [Fact]
        public void EncryptString_DifferentInstances_ShouldProduceDifferentResults()
        {
            // Arrange
            var encryption1 = new EncryptionService();
            var encryption2 = new EncryptionService();
            var plainText = "test-data";

            // Act
            var encrypted1 = encryption1.EncryptString(plainText);
            var encrypted2 = encryption2.EncryptString(plainText);

            // Assert
            encrypted1.Should().NotBe(encrypted2); // Diferentes claves = diferentes resultados
        }
    }
}
