using FluentAssertions;
using JonjubNet.Observability.Shared.Context;
using Xunit;

namespace JonjubNet.Observability.Shared.Context.Tests
{
    /// <summary>
    /// Pruebas para CorrelationPropagationHelper
    /// </summary>
    public class CorrelationPropagationHelperTests
    {
        [Fact]
        public void GetCorrelationId_WhenContextIsNull_ShouldReturnNull()
        {
            // Arrange
            ObservabilityContext.Clear();

            // Act
            var result = CorrelationPropagationHelper.GetCorrelationId();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetCorrelationId_WhenContextHasCorrelationId_ShouldReturnCorrelationId()
        {
            // Arrange
            var correlationId = "test-correlation-id-123";
            ObservabilityContext.SetCorrelationId(correlationId);

            // Act
            var result = CorrelationPropagationHelper.GetCorrelationId();

            // Assert
            result.Should().Be(correlationId);
        }

        [Fact]
        public void CreateHeadersWithCorrelationId_WhenCorrelationIdProvided_ShouldCreateHeaders()
        {
            // Arrange
            var correlationId = "test-correlation-id-456";

            // Act
            var headers = CorrelationPropagationHelper.CreateHeadersWithCorrelationId(correlationId);

            // Assert
            headers.Should().NotBeNull();
            headers.Should().ContainKey(CorrelationPropagationHelper.CorrelationIdHeaderName);
            headers[CorrelationPropagationHelper.CorrelationIdHeaderName].Should().Be(correlationId);
            headers.Count.Should().Be(1);
        }

        [Fact]
        public void CreateHeadersWithCorrelationId_WhenCorrelationIdIsNull_ShouldReturnNull()
        {
            // Arrange
            ObservabilityContext.Clear();

            // Act
            var headers = CorrelationPropagationHelper.CreateHeadersWithCorrelationId(null);

            // Assert
            headers.Should().BeNull();
        }

        [Fact]
        public void CreateHeadersWithCorrelationId_WhenNoCorrelationIdProvidedAndContextHasIt_ShouldUseContext()
        {
            // Arrange
            var correlationId = "test-correlation-id-789";
            ObservabilityContext.SetCorrelationId(correlationId);

            // Act
            var headers = CorrelationPropagationHelper.CreateHeadersWithCorrelationId();

            // Assert
            headers.Should().NotBeNull();
            headers.Should().ContainKey(CorrelationPropagationHelper.CorrelationIdHeaderName);
            headers[CorrelationPropagationHelper.CorrelationIdHeaderName].Should().Be(correlationId);
        }

        [Fact]
        public void AddCorrelationIdToHeaders_WhenHeadersIsNull_ShouldNotThrow()
        {
            // Arrange
            Dictionary<string, string>? headers = null;

            // Act & Assert
            var act = () => CorrelationPropagationHelper.AddCorrelationIdToHeaders(headers!);
            act.Should().NotThrow();
        }

        [Fact]
        public void AddCorrelationIdToHeaders_WhenHeadersIsNotNull_ShouldAddCorrelationId()
        {
            // Arrange
            var headers = new Dictionary<string, string>();
            var correlationId = "test-correlation-id-999";

            // Act
            CorrelationPropagationHelper.AddCorrelationIdToHeaders(headers, correlationId);

            // Assert
            headers.Should().ContainKey(CorrelationPropagationHelper.CorrelationIdHeaderName);
            headers[CorrelationPropagationHelper.CorrelationIdHeaderName].Should().Be(correlationId);
        }

        [Fact]
        public void AddCorrelationIdToHeaders_WhenCorrelationIdProvided_ShouldAddToHeaders()
        {
            // Arrange
            var headers = new Dictionary<string, string>();
            var correlationId = "test-correlation-id-101";

            // Act
            CorrelationPropagationHelper.AddCorrelationIdToHeaders(headers, correlationId);

            // Assert
            headers.Should().ContainKey(CorrelationPropagationHelper.CorrelationIdHeaderName);
            headers[CorrelationPropagationHelper.CorrelationIdHeaderName].Should().Be(correlationId);
        }

        [Fact]
        public void AddCorrelationIdToHeaders_WhenNoCorrelationIdProvidedAndContextHasIt_ShouldUseContext()
        {
            // Arrange
            var headers = new Dictionary<string, string>();
            var correlationId = "test-correlation-id-202";
            ObservabilityContext.SetCorrelationId(correlationId);

            // Act
            CorrelationPropagationHelper.AddCorrelationIdToHeaders(headers);

            // Assert
            headers.Should().ContainKey(CorrelationPropagationHelper.CorrelationIdHeaderName);
            headers[CorrelationPropagationHelper.CorrelationIdHeaderName].Should().Be(correlationId);
        }

        [Fact]
        public void ExtractCorrelationIdFromHeaders_WhenHeadersIsNull_ShouldReturnNull()
        {
            // Arrange
            Dictionary<string, string>? headers = null;

            // Act
            var result = CorrelationPropagationHelper.ExtractCorrelationIdFromHeaders(headers);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ExtractCorrelationIdFromHeaders_WhenHeadersIsEmpty_ShouldReturnNull()
        {
            // Arrange
            var headers = new Dictionary<string, string>();

            // Act
            var result = CorrelationPropagationHelper.ExtractCorrelationIdFromHeaders(headers);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ExtractCorrelationIdFromHeaders_WhenHeaderExists_ShouldReturnCorrelationId()
        {
            // Arrange
            var correlationId = "test-correlation-id-303";
            var headers = new Dictionary<string, string>
            {
                { CorrelationPropagationHelper.CorrelationIdHeaderName, correlationId }
            };

            // Act
            var result = CorrelationPropagationHelper.ExtractCorrelationIdFromHeaders(headers);

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(correlationId);
        }

        [Fact]
        public void ExtractCorrelationIdFromHeaders_WhenHeaderExistsCaseInsensitive_ShouldReturnCorrelationId()
        {
            // Arrange
            var correlationId = "test-correlation-id-404";
            var headers = new Dictionary<string, string>
            {
                { "x-correlation-id", correlationId } // lowercase
            };

            // Act
            var result = CorrelationPropagationHelper.ExtractCorrelationIdFromHeaders(headers);

            // Assert
            result.Should().Be(correlationId);
        }

        [Fact]
        public void ExtractCorrelationIdFromHeaders_WhenHeaderDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "other-header", "value" }
            };

            // Act
            var result = CorrelationPropagationHelper.ExtractCorrelationIdFromHeaders(headers);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void CorrelationIdHeaderName_ShouldBeXCorrelationId()
        {
            // Act & Assert
            CorrelationPropagationHelper.CorrelationIdHeaderName.Should().Be("X-Correlation-Id");
        }
    }
}

