using FluentAssertions;
using JonjubNet.Observability.Shared.Context;
using JonjubNet.Observability.Shared.Context.Protocols;
using Xunit;

namespace JonjubNet.Observability.Shared.Context.Tests.Protocols
{
    /// <summary>
    /// Pruebas para GrpcCorrelationHelper
    /// </summary>
    public class GrpcCorrelationHelperTests
    {
        [Fact]
        public void CreateMetadata_WhenContextIsNull_ShouldReturnNull()
        {
            // Arrange
            ObservabilityContext.Clear();

            // Act
            var metadata = GrpcCorrelationHelper.CreateMetadata();

            // Assert
            metadata.Should().BeNull();
        }

        [Fact]
        public void CreateMetadata_WhenContextHasCorrelationId_ShouldCreateMetadata()
        {
            // Arrange
            var correlationId = "test-correlation-id-123";
            ObservabilityContext.SetCorrelationId(correlationId);

            // Act
            var metadata = GrpcCorrelationHelper.CreateMetadata();

            // Assert
            metadata.Should().NotBeNull();
            metadata!.Should().ContainKey(CorrelationPropagationHelper.CorrelationIdHeaderName);
            metadata[CorrelationPropagationHelper.CorrelationIdHeaderName].Should().Be(correlationId);
            metadata.Count.Should().Be(1);
        }

        [Fact]
        public void CreateMetadata_WhenCorrelationIdProvided_ShouldCreateMetadata()
        {
            // Arrange
            var correlationId = "test-correlation-id-456";

            // Act
            var metadata = GrpcCorrelationHelper.CreateMetadata(correlationId);

            // Assert
            metadata.Should().NotBeNull();
            metadata.Should().ContainKey(CorrelationPropagationHelper.CorrelationIdHeaderName);
            metadata[CorrelationPropagationHelper.CorrelationIdHeaderName].Should().Be(correlationId);
        }

        [Fact]
        public void CreateMetadata_WhenCorrelationIdIsNull_ShouldReturnNull()
        {
            // Act
            var metadata = GrpcCorrelationHelper.CreateMetadata((string?)null);

            // Assert
            metadata.Should().BeNull();
        }

        [Fact]
        public void AddCorrelationIdToMetadata_WhenMetadataIsNull_ShouldNotThrow()
        {
            // Arrange
            Dictionary<string, string>? metadata = null;

            // Act & Assert
            var act = () => GrpcCorrelationHelper.AddCorrelationIdToMetadata(metadata!);
            act.Should().NotThrow();
        }

        [Fact]
        public void AddCorrelationIdToMetadata_WhenMetadataIsNotNull_ShouldAddCorrelationId()
        {
            // Arrange
            var metadata = new Dictionary<string, string>();
            var correlationId = "test-correlation-id-888";

            // Act
            GrpcCorrelationHelper.AddCorrelationIdToMetadata(metadata, correlationId);

            // Assert
            metadata.Should().ContainKey(CorrelationPropagationHelper.CorrelationIdHeaderName);
            metadata[CorrelationPropagationHelper.CorrelationIdHeaderName].Should().Be(correlationId);
        }

        [Fact]
        public void AddCorrelationIdToMetadata_WhenCorrelationIdProvided_ShouldAddToMetadata()
        {
            // Arrange
            var metadata = new Dictionary<string, string>();
            var correlationId = "test-correlation-id-789";

            // Act
            GrpcCorrelationHelper.AddCorrelationIdToMetadata(metadata, correlationId);

            // Assert
            metadata.Should().ContainKey(CorrelationPropagationHelper.CorrelationIdHeaderName);
            metadata[CorrelationPropagationHelper.CorrelationIdHeaderName].Should().Be(correlationId);
        }

        [Fact]
        public void AddCorrelationIdToMetadata_WhenNoCorrelationIdProvidedAndContextHasIt_ShouldUseContext()
        {
            // Arrange
            var metadata = new Dictionary<string, string>();
            var correlationId = "test-correlation-id-101";
            ObservabilityContext.SetCorrelationId(correlationId);

            // Act
            GrpcCorrelationHelper.AddCorrelationIdToMetadata(metadata);

            // Assert
            metadata.Should().ContainKey(CorrelationPropagationHelper.CorrelationIdHeaderName);
            metadata[CorrelationPropagationHelper.CorrelationIdHeaderName].Should().Be(correlationId);
        }

        [Fact]
        public void ExtractCorrelationIdFromMetadata_WhenMetadataIsNull_ShouldReturnNull()
        {
            // Arrange
            Dictionary<string, string>? metadata = null;

            // Act
            var result = GrpcCorrelationHelper.ExtractCorrelationIdFromMetadata(metadata);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ExtractCorrelationIdFromMetadata_WhenMetadataIsEmpty_ShouldReturnNull()
        {
            // Arrange
            var metadata = new Dictionary<string, string>();

            // Act
            var result = GrpcCorrelationHelper.ExtractCorrelationIdFromMetadata(metadata);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ExtractCorrelationIdFromMetadata_WhenHeaderExists_ShouldReturnCorrelationId()
        {
            // Arrange
            var correlationId = "test-correlation-id-202";
            var metadata = new Dictionary<string, string>
            {
                { CorrelationPropagationHelper.CorrelationIdHeaderName, correlationId }
            };

            // Act
            var result = GrpcCorrelationHelper.ExtractCorrelationIdFromMetadata(metadata);

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(correlationId);
        }
    }
}

