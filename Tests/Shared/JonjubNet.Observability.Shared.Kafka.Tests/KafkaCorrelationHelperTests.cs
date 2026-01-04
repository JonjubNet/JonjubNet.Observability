using Confluent.Kafka;
using FluentAssertions;
using JonjubNet.Observability.Shared.Context;
using JonjubNet.Observability.Shared.Kafka;
using Xunit;

namespace JonjubNet.Observability.Shared.Kafka.Tests
{
    /// <summary>
    /// Pruebas para KafkaCorrelationHelper
    /// </summary>
    public class KafkaCorrelationHelperTests
    {
        [Fact]
        public void CreateKafkaHeaders_WhenContextIsNull_ShouldReturnNull()
        {
            // Arrange
            ObservabilityContext.Clear();

            // Act
            var headers = KafkaCorrelationHelper.CreateKafkaHeaders();

            // Assert
            headers.Should().BeNull();
        }

        [Fact]
        public void CreateKafkaHeaders_WhenContextHasCorrelationId_ShouldCreateHeaders()
        {
            // Arrange
            var correlationId = "test-correlation-id-123";
            ObservabilityContext.SetCorrelationId(correlationId);

            // Act
            var headers = KafkaCorrelationHelper.CreateKafkaHeaders();

            // Assert
            headers.Should().NotBeNull();
            headers!.Count.Should().Be(1);
            
            var extractedId = KafkaCorrelationHelper.ExtractCorrelationIdFromHeaders(headers);
            extractedId.Should().Be(correlationId);
        }

        [Fact]
        public void CreateKafkaHeaders_WhenCorrelationIdProvided_ShouldCreateHeaders()
        {
            // Arrange
            var correlationId = "test-correlation-id-456";

            // Act
            var headers = KafkaCorrelationHelper.CreateKafkaHeaders(correlationId);

            // Assert
            headers.Should().NotBeNull();
            headers!.Count.Should().Be(1);
            
            var extractedId = KafkaCorrelationHelper.ExtractCorrelationIdFromHeaders(headers);
            extractedId.Should().Be(correlationId);
        }

        [Fact]
        public void CreateKafkaHeaders_WhenCorrelationIdIsNull_ShouldReturnNull()
        {
            // Act
            string? nullId = null;
            var headers = KafkaCorrelationHelper.CreateKafkaHeaders(nullId);

            // Assert
            headers.Should().BeNull();
        }

        [Fact]
        public void CreateKafkaHeaders_WhenCorrelationIdIsEmpty_ShouldReturnNull()
        {
            // Act
            var headers = KafkaCorrelationHelper.CreateKafkaHeaders(string.Empty);

            // Assert
            headers.Should().BeNull();
        }

        [Fact]
        public void AddCorrelationIdToHeaders_WhenHeadersIsNull_ShouldNotThrow()
        {
            // Arrange
            Headers? headers = null;

            // Act & Assert
            var act = () => KafkaCorrelationHelper.AddCorrelationIdToHeaders(headers!);
            act.Should().NotThrow();
        }

        [Fact]
        public void AddCorrelationIdToHeaders_WhenHeadersIsNotNull_ShouldAddCorrelationId()
        {
            // Arrange
            var headers = new Headers();
            var correlationId = "test-correlation-id-777";

            // Act
            KafkaCorrelationHelper.AddCorrelationIdToHeaders(headers, correlationId);

            // Assert
            headers.Count.Should().Be(1);
            var extractedId = KafkaCorrelationHelper.ExtractCorrelationIdFromHeaders(headers);
            extractedId.Should().Be(correlationId);
        }

        [Fact]
        public void AddCorrelationIdToHeaders_WhenCorrelationIdProvided_ShouldAddToHeaders()
        {
            // Arrange
            var headers = new Headers();
            var correlationId = "test-correlation-id-789";

            // Act
            KafkaCorrelationHelper.AddCorrelationIdToHeaders(headers, correlationId);

            // Assert
            headers.Count.Should().Be(1);
            var extractedId = KafkaCorrelationHelper.ExtractCorrelationIdFromHeaders(headers);
            extractedId.Should().Be(correlationId);
        }

        [Fact]
        public void ExtractCorrelationIdFromHeaders_WhenHeadersIsNull_ShouldReturnNull()
        {
            // Arrange
            Headers? headers = null;

            // Act
            var result = KafkaCorrelationHelper.ExtractCorrelationIdFromHeaders(headers);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ExtractCorrelationIdFromHeaders_WhenHeadersIsEmpty_ShouldReturnNull()
        {
            // Arrange
            var headers = new Headers();

            // Act
            var result = KafkaCorrelationHelper.ExtractCorrelationIdFromHeaders(headers);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ExtractCorrelationIdFromHeaders_WhenHeaderExists_ShouldReturnCorrelationId()
        {
            // Arrange
            var correlationId = "test-correlation-id-202";
            var headers = new Headers();
            headers.Add(CorrelationPropagationHelper.CorrelationIdHeaderName, System.Text.Encoding.UTF8.GetBytes(correlationId));

            // Act
            var result = KafkaCorrelationHelper.ExtractCorrelationIdFromHeaders(headers);

            // Assert
            result.Should().Be(correlationId);
        }

        [Fact]
        public void ExtractCorrelationIdFromHeaders_WhenHeaderExistsCaseInsensitive_ShouldReturnCorrelationId()
        {
            // Arrange
            var correlationId = "test-correlation-id-303";
            var headers = new Headers();
            headers.Add("x-correlation-id", System.Text.Encoding.UTF8.GetBytes(correlationId)); // lowercase

            // Act
            var result = KafkaCorrelationHelper.ExtractCorrelationIdFromHeaders(headers);

            // Assert
            result.Should().Be(correlationId);
        }

        [Fact]
        public void ExtractCorrelationIdFromHeaders_WhenHeaderDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var headers = new Headers();
            headers.Add("other-header", System.Text.Encoding.UTF8.GetBytes("value"));

            // Act
            var result = KafkaCorrelationHelper.ExtractCorrelationIdFromHeaders(headers);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ExtractCorrelationIdFromHeaders_WhenMultipleHeadersExist_ShouldReturnCorrelationId()
        {
            // Arrange
            var correlationId = "test-correlation-id-404";
            var headers = new Headers();
            headers.Add("other-header", System.Text.Encoding.UTF8.GetBytes("value"));
            headers.Add(CorrelationPropagationHelper.CorrelationIdHeaderName, System.Text.Encoding.UTF8.GetBytes(correlationId));
            headers.Add("another-header", System.Text.Encoding.UTF8.GetBytes("another-value"));

            // Act
            var result = KafkaCorrelationHelper.ExtractCorrelationIdFromHeaders(headers);

            // Assert
            result.Should().Be(correlationId);
        }
    }
}

