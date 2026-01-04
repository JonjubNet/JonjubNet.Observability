using FluentAssertions;
using JonjubNet.Observability.Shared.Context;
using JonjubNet.Observability.Shared.Context.Protocols;
using Xunit;

namespace JonjubNet.Observability.Shared.Context.Tests.Protocols
{
    /// <summary>
    /// Pruebas para RabbitMqCorrelationHelper
    /// </summary>
    public class RabbitMqCorrelationHelperTests
    {
        [Fact]
        public void CreateProperties_WhenContextIsNull_ShouldReturnNull()
        {
            // Arrange
            ObservabilityContext.Clear();

            // Act
            var properties = RabbitMqCorrelationHelper.CreateProperties();

            // Assert
            properties.Should().BeNull();
        }

        [Fact]
        public void CreateProperties_WhenContextHasCorrelationId_ShouldCreateProperties()
        {
            // Arrange
            var correlationId = "test-correlation-id-123";
            ObservabilityContext.SetCorrelationId(correlationId);

            // Act
            var properties = RabbitMqCorrelationHelper.CreateProperties();

            // Assert
            properties.Should().NotBeNull();
            properties!.Should().ContainKey(CorrelationPropagationHelper.CorrelationIdHeaderName);
            properties[CorrelationPropagationHelper.CorrelationIdHeaderName].Should().Be(correlationId);
            properties.Count.Should().Be(1);
        }

        [Fact]
        public void AddCorrelationIdToProperties_WhenPropertiesIsNull_ShouldNotThrow()
        {
            // Arrange
            IDictionary<string, object>? properties = null;

            // Act & Assert
            var act = () => RabbitMqCorrelationHelper.AddCorrelationIdToProperties(properties!);
            act.Should().NotThrow();
        }

        [Fact]
        public void AddCorrelationIdToProperties_WhenCorrelationIdProvided_ShouldAddToProperties()
        {
            // Arrange
            var properties = new Dictionary<string, object>();
            var correlationId = "test-correlation-id-456";

            // Act
            RabbitMqCorrelationHelper.AddCorrelationIdToProperties(properties, correlationId);

            // Assert
            properties.Should().ContainKey(CorrelationPropagationHelper.CorrelationIdHeaderName);
            properties[CorrelationPropagationHelper.CorrelationIdHeaderName].Should().Be(correlationId);
        }

        [Fact]
        public void AddCorrelationIdToProperties_WhenNoCorrelationIdProvidedAndContextHasIt_ShouldUseContext()
        {
            // Arrange
            var properties = new Dictionary<string, object>();
            var correlationId = "test-correlation-id-789";
            ObservabilityContext.SetCorrelationId(correlationId);

            // Act
            RabbitMqCorrelationHelper.AddCorrelationIdToProperties(properties);

            // Assert
            properties.Should().ContainKey(CorrelationPropagationHelper.CorrelationIdHeaderName);
            properties[CorrelationPropagationHelper.CorrelationIdHeaderName].Should().Be(correlationId);
        }

        [Fact]
        public void ExtractCorrelationIdFromProperties_WhenPropertiesIsNull_ShouldReturnNull()
        {
            // Arrange
            IDictionary<string, object>? properties = null;

            // Act
            var result = RabbitMqCorrelationHelper.ExtractCorrelationIdFromProperties(properties);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ExtractCorrelationIdFromProperties_WhenPropertiesIsEmpty_ShouldReturnNull()
        {
            // Arrange
            var properties = new Dictionary<string, object>();

            // Act
            var result = RabbitMqCorrelationHelper.ExtractCorrelationIdFromProperties(properties);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ExtractCorrelationIdFromProperties_WhenHeaderExists_ShouldReturnCorrelationId()
        {
            // Arrange
            var correlationId = "test-correlation-id-101";
            var properties = new Dictionary<string, object>
            {
                { CorrelationPropagationHelper.CorrelationIdHeaderName, correlationId }
            };

            // Act
            var result = RabbitMqCorrelationHelper.ExtractCorrelationIdFromProperties(properties);

            // Assert
            result.Should().Be(correlationId);
        }

        [Fact]
        public void ExtractCorrelationIdFromProperties_WhenHeaderExistsCaseInsensitive_ShouldReturnCorrelationId()
        {
            // Arrange
            var correlationId = "test-correlation-id-202";
            var properties = new Dictionary<string, object>
            {
                { "x-correlation-id", correlationId } // lowercase
            };

            // Act
            var result = RabbitMqCorrelationHelper.ExtractCorrelationIdFromProperties(properties);

            // Assert
            result.Should().Be(correlationId);
        }

        [Fact]
        public void ExtractCorrelationIdFromProperties_WhenHeaderDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var properties = new Dictionary<string, object>
            {
                { "other-header", "value" }
            };

            // Act
            var result = RabbitMqCorrelationHelper.ExtractCorrelationIdFromProperties(properties);

            // Assert
            result.Should().BeNull();
        }
    }
}

