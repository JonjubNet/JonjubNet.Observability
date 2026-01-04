using FluentAssertions;
using JonjubNet.Observability.Shared.Utils;
using System.Text;
using Xunit;

namespace JonjubNet.Observability.Metrics.Shared.Tests.Utils
{
    public class CompressionHelperTests
    {
        [Fact]
        public void CompressGZip_AndDecompressGZip_ShouldRoundTrip()
        {
            // Arrange
            var original = "test data for compression";
            var bytes = Encoding.UTF8.GetBytes(original);

            // Act
            var compressed = CompressionHelper.CompressGZip(bytes);
            var decompressed = CompressionHelper.DecompressGZip(compressed);
            var result = Encoding.UTF8.GetString(decompressed);

            // Assert
            compressed.Length.Should().BeLessThan(bytes.Length);
            result.Should().Be(original);
        }

        [Fact]
        public void CompressGZip_String_AndDecompressGZipToString_ShouldRoundTrip()
        {
            // Arrange
            var original = "test data for compression";

            // Act
            var compressed = CompressionHelper.CompressGZipString(original);
            var decompressed = CompressionHelper.DecompressGZipString(compressed);

            // Assert
            decompressed.Should().Be(original);
        }

        [Fact]
        public void CompressBrotli_AndDecompressBrotli_ShouldRoundTrip()
        {
            // Arrange
            var original = "test data for compression";
            var bytes = Encoding.UTF8.GetBytes(original);

            // Act
            var compressed = CompressionHelper.CompressBrotli(bytes);
            var decompressed = CompressionHelper.DecompressBrotli(compressed);
            var result = Encoding.UTF8.GetString(decompressed);

            // Assert
            compressed.Length.Should().BeLessThan(bytes.Length);
            result.Should().Be(original);
        }

        [Fact]
        public void CompressBrotli_ShouldCompressBetterThanGZip()
        {
            // Arrange
            var data = new string('a', 10000);
            var bytes = Encoding.UTF8.GetBytes(data);

            // Act
            var gzipCompressed = CompressionHelper.CompressGZip(bytes);
            var brotliCompressed = CompressionHelper.CompressBrotli(bytes);

            // Assert
            // Brotli generalmente comprime mejor que GZip para datos repetitivos
            brotliCompressed.Length.Should().BeLessThanOrEqualTo(gzipCompressed.Length);
        }
    }
}

