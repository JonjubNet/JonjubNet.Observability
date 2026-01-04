using FluentAssertions;
using JonjubNet.Observability.Logging.Core;
using JonjubNet.Observability.Logging.Core.Interfaces;
using Xunit;
using CoreLogLevel = JonjubNet.Observability.Logging.Core.LogLevel;

namespace JonjubNet.Observability.Logging.Integration.Tests
{
    public class LoggingIntegrationTests
    {
        [Fact]
        public void LoggingClient_WithRegistry_ShouldWorkEndToEnd()
        {
            // Arrange
            var registry = new LogRegistry();
            var client = new LoggingClient(registry);

            // Act
            client.LogInformation("Test message", "TestCategory");
            client.LogWarning("Warning message");
            client.LogError("Error message", new InvalidOperationException("Test error"));

            // Assert
            registry.Count.Should().Be(3);
            var logs = registry.GetAllLogs();
            logs.Should().HaveCount(3);
            logs[0].Level.Should().Be(CoreLogLevel.Information);
            logs[1].Level.Should().Be(CoreLogLevel.Warning);
            logs[2].Level.Should().Be(CoreLogLevel.Error);
        }

        [Fact]
        public void LoggingClient_WithScope_ShouldIncludeScopeProperties()
        {
            // Arrange
            var registry = new LogRegistry();
            var client = new LoggingClient(registry);

            // Act
            using (client.BeginScope("TestScope", new Dictionary<string, object?> { ["scopeProp"] = "scopeValue" }))
            {
                client.LogInformation("Message in scope");
            }

            // Assert
            var logs = registry.GetAllLogs();
            logs[0].Properties.Should().ContainKey("scopeProp");
            logs[0].Properties["scopeProp"].Should().Be("scopeValue");
        }

        [Fact]
        public void LoggingClient_WithFilter_ShouldFilterLogs()
        {
            // Arrange
            var registry = new LogRegistry();
            var filter = new Logging.Core.Filters.LogFilter(
                new Logging.Core.Filters.FilterOptions { MinLevel = CoreLogLevel.Warning });
            var client = new LoggingClient(registry, filter: filter);

            // Act
            client.LogInformation("Info message"); // Debe ser filtrado
            client.LogWarning("Warning message"); // Debe pasar

            // Assert
            registry.Count.Should().Be(1);
            var logs = registry.GetAllLogs();
            logs[0].Level.Should().Be(CoreLogLevel.Warning);
        }

        [Fact]
        public void LoggingClient_WithEnricher_ShouldEnrichLogs()
        {
            // Arrange
            var registry = new LogRegistry();
            var enricher = new Logging.Core.Enrichment.LogEnricher(
                new Logging.Core.Enrichment.EnrichmentOptions 
                { 
                    IncludeEnvironment = true, 
                    Environment = "Test" 
                });
            var client = new LoggingClient(registry, enricher: enricher);

            // Act
            client.LogInformation("Test message");

            // Assert
            var logs = registry.GetAllLogs();
            logs[0].Properties.Should().ContainKey("Environment");
            logs[0].Properties["Environment"].Should().Be("Test");
        }
    }
}

