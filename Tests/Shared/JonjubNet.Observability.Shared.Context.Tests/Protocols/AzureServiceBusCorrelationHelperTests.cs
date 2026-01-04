using FluentAssertions;
using JonjubNet.Observability.Shared.Context;
using JonjubNet.Observability.Shared.Context.Protocols;
using Xunit;

namespace JonjubNet.Observability.Shared.Context.Tests.Protocols
{
    /// <summary>
    /// Pruebas para AzureServiceBusCorrelationHelper
    /// </summary>
    public class AzureServiceBusCorrelationHelperTests
    {
        [Fact]
        public void CreateApplicationProperties_WhenContextIsNull_ShouldReturnNull()
        {
            // Arrange
            ObservabilityContext.Clear();

            // Act
            var properties = AzureServiceBusCorrelationHelper.CreateApplicationProperties();

            // Assert
            properties.Should().BeNull();
        }

        [Fact]
        public void CreateApplicationProperties_WhenContextHasCorrelationId_ShouldCreateProperties()
        {
            // Arrange
            var correlationId = "test-correlation-id-123";
            ObservabilityContext.SetCorrelationId(correlationId);

            // Act
            var properties = AzureServiceBusCorrelationHelper.CreateApplicationProperties();

            // Assert
            properties.Should().NotBeNull();
            properties!.Should().ContainKey(CorrelationPropagationHelper.CorrelationIdHeaderName);
            properties[CorrelationPropagationHelper.CorrelationIdHeaderName].Should().Be(correlationId);
            properties.Count.Should().Be(1);
        }

        [Fact]
        public void AddCorrelationIdToApplicationProperties_WhenPropertiesIsNull_ShouldNotThrow()
        {
            // Arrange
            IDictionary<string, object>? properties = null;

            // Act & Assert
            var act = () => AzureServiceBusCorrelationHelper.AddCorrelationIdToApplicationProperties(properties!);
            act.Should().NotThrow();
        }

        [Fact]
        public void AddCorrelationIdToApplicationProperties_WhenCorrelationIdProvided_ShouldAddToProperties()
        {
            // Arrange
            var properties = new Dictionary<string, object>();
            var correlationId = "test-correlation-id-456";

            // Act
            AzureServiceBusCorrelationHelper.AddCorrelationIdToApplicationProperties(properties, correlationId);

            // Assert
            properties.Should().ContainKey(CorrelationPropagationHelper.CorrelationIdHeaderName);
            properties[CorrelationPropagationHelper.CorrelationIdHeaderName].Should().Be(correlationId);
        }

        [Fact]
        public void AddCorrelationIdToApplicationProperties_WhenNoCorrelationIdProvidedAndContextHasIt_ShouldUseContext()
        {
            // Arrange
            var properties = new Dictionary<string, object>();
            var correlationId = "test-correlation-id-789";
            ObservabilityContext.SetCorrelationId(correlationId);

            // Act
            AzureServiceBusCorrelationHelper.AddCorrelationIdToApplicationProperties(properties);

            // Assert
            properties.Should().ContainKey(CorrelationPropagationHelper.CorrelationIdHeaderName);
            properties[CorrelationPropagationHelper.CorrelationIdHeaderName].Should().Be(correlationId);
        }

        [Fact]
        public void ExtractCorrelationIdFromApplicationProperties_WhenPropertiesIsNull_ShouldReturnNull()
        {
            // Arrange
            IDictionary<string, object>? properties = null;

            // Act
            var result = AzureServiceBusCorrelationHelper.ExtractCorrelationIdFromApplicationProperties(properties);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ExtractCorrelationIdFromApplicationProperties_WhenPropertiesIsEmpty_ShouldReturnNull()
        {
            // Arrange
            var properties = new Dictionary<string, object>();

            // Act
            var result = AzureServiceBusCorrelationHelper.ExtractCorrelationIdFromApplicationProperties(properties);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ExtractCorrelationIdFromApplicationProperties_WhenHeaderExists_ShouldReturnCorrelationId()
        {
            // Arrange
            var correlationId = "test-correlation-id-101";
            var properties = new Dictionary<string, object>
            {
                { CorrelationPropagationHelper.CorrelationIdHeaderName, correlationId }
            };

            // Act
            var result = AzureServiceBusCorrelationHelper.ExtractCorrelationIdFromApplicationProperties(properties);

            // Assert
            result.Should().Be(correlationId);
        }
    }
}

