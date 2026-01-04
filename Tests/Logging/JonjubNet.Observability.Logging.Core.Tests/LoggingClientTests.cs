using FluentAssertions;
using JonjubNet.Observability.Logging.Core;
using JonjubNet.Observability.Logging.Core.Filters;
using JonjubNet.Observability.Logging.Core.Enrichment;
using Xunit;
using CoreLogLevel = JonjubNet.Observability.Logging.Core.LogLevel;

namespace JonjubNet.Observability.Logging.Core.Tests
{
    public class LoggingClientTests
    {
        [Fact]
        public void LogTrace_ShouldAddLogToRegistry()
        {
            // Arrange
            var registry = new LogRegistry();
            var client = new LoggingClient(registry);

            // Act
            client.LogTrace("Test trace message", "TestCategory");

            // Assert
            registry.Count.Should().Be(1);
            var logs = registry.GetAllLogs();
            logs.Should().HaveCount(1);
            logs[0].Level.Should().Be(CoreLogLevel.Trace);
            logs[0].Message.Should().Be("Test trace message");
            logs[0].Category.Should().Be("TestCategory");
        }

        [Fact]
        public void LogDebug_ShouldAddLogToRegistry()
        {
            // Arrange
            var registry = new LogRegistry();
            var client = new LoggingClient(registry);

            // Act
            client.LogDebug("Test debug message");

            // Assert
            registry.Count.Should().Be(1);
            var logs = registry.GetAllLogs();
            logs[0].Level.Should().Be(CoreLogLevel.Debug);
            logs[0].Message.Should().Be("Test debug message");
        }

        [Fact]
        public void LogInformation_ShouldAddLogToRegistry()
        {
            // Arrange
            var registry = new LogRegistry();
            var client = new LoggingClient(registry);

            // Act
            client.LogInformation("Test info message", "Category1");

            // Assert
            registry.Count.Should().Be(1);
            var logs = registry.GetAllLogs();
            logs[0].Level.Should().Be(CoreLogLevel.Information);
            logs[0].Message.Should().Be("Test info message");
            logs[0].Category.Should().Be("Category1");
        }

        [Fact]
        public void LogWarning_ShouldAddLogToRegistry()
        {
            // Arrange
            var registry = new LogRegistry();
            var client = new LoggingClient(registry);

            // Act
            client.LogWarning("Test warning message");

            // Assert
            registry.Count.Should().Be(1);
            var logs = registry.GetAllLogs();
            logs[0].Level.Should().Be(CoreLogLevel.Warning);
        }

        [Fact]
        public void LogError_ShouldAddLogToRegistry_WithException()
        {
            // Arrange
            var registry = new LogRegistry();
            var client = new LoggingClient(registry);
            var exception = new InvalidOperationException("Test exception");

            // Act
            client.LogError("Test error message", exception);

            // Assert
            registry.Count.Should().Be(1);
            var logs = registry.GetAllLogs();
            logs[0].Level.Should().Be(CoreLogLevel.Error);
            logs[0].Exception.Should().Be(exception);
        }

        [Fact]
        public void LogCritical_ShouldAddLogToRegistry_WithException()
        {
            // Arrange
            var registry = new LogRegistry();
            var client = new LoggingClient(registry);
            var exception = new InvalidOperationException("Critical exception");

            // Act
            client.LogCritical("Test critical message", exception);

            // Assert
            registry.Count.Should().Be(1);
            var logs = registry.GetAllLogs();
            logs[0].Level.Should().Be(CoreLogLevel.Critical);
            logs[0].Exception.Should().Be(exception);
        }

        [Fact]
        public void Log_WithProperties_ShouldIncludeProperties()
        {
            // Arrange
            var registry = new LogRegistry();
            var client = new LoggingClient(registry);
            var properties = new Dictionary<string, object?> { ["key1"] = "value1", ["key2"] = 42 };

            // Act
            client.LogInformation("Test message", properties: properties);

            // Assert
            var logs = registry.GetAllLogs();
            logs[0].Properties.Should().ContainKey("key1");
            logs[0].Properties.Should().ContainKey("key2");
            logs[0].Properties["key1"].Should().Be("value1");
            logs[0].Properties["key2"].Should().Be(42);
        }

        [Fact]
        public void Log_WithTags_ShouldIncludeTags()
        {
            // Arrange
            var registry = new LogRegistry();
            var client = new LoggingClient(registry);
            var tags = new Dictionary<string, string> { ["env"] = "prod", ["service"] = "api" };

            // Act
            client.LogInformation("Test message", tags: tags);

            // Assert
            var logs = registry.GetAllLogs();
            logs[0].Tags.Should().ContainKey("env");
            logs[0].Tags.Should().ContainKey("service");
            logs[0].Tags["env"].Should().Be("prod");
            logs[0].Tags["service"].Should().Be("api");
        }

        [Fact]
        public void Log_WithFilter_ShouldFilterLogs()
        {
            // Arrange
            var registry = new LogRegistry();
            var filterOptions = new Logging.Core.Filters.FilterOptions { MinLevel = CoreLogLevel.Warning };
            var filter = new Logging.Core.Filters.LogFilter(filterOptions); // Solo Warning y superiores
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
        public void Log_WithEnricher_ShouldEnrichLogs()
        {
            // Arrange
            var registry = new LogRegistry();
            var enrichmentOptions = new Logging.Core.Enrichment.EnrichmentOptions
            {
                CustomProperties = new Dictionary<string, object?> { ["enriched"] = "true" }
            };
            var enricher = new Logging.Core.Enrichment.LogEnricher(enrichmentOptions);
            var client = new LoggingClient(registry, enricher: enricher);

            // Act
            client.LogInformation("Test message");

            // Assert
            var logs = registry.GetAllLogs();
            logs[0].Properties.Should().ContainKey("enriched");
            logs[0].Properties["enriched"].Should().Be("true");
        }

        [Fact]
        public void BeginScope_ShouldCreateScope()
        {
            // Arrange
            var registry = new LogRegistry();
            var client = new LoggingClient(registry);
            var scopeProperties = new Dictionary<string, object?> { ["scope"] = "test" };

            // Act
            using (client.BeginScope("TestScope", scopeProperties))
            {
                client.LogInformation("Message in scope");
            }

            // Assert
            var logs = registry.GetAllLogs();
            logs[0].Properties.Should().ContainKey("scope");
            logs[0].Properties["scope"].Should().Be("test");
        }

        [Fact]
        public void BeginOperation_ShouldLogOperationCompletion()
        {
            // Arrange
            var registry = new LogRegistry();
            var client = new LoggingClient(registry);

            // Act
            using (client.BeginOperation("TestOperation"))
            {
                System.Threading.Thread.Sleep(10);
            }

            // Assert
            var logs = registry.GetAllLogs();
            logs.Should().HaveCountGreaterThan(0);
            var operationLog = logs.FirstOrDefault(l => l.Message.Contains("TestOperation"));
            operationLog.Should().NotBeNull();
            operationLog!.Properties.Should().ContainKey("DurationMs");
        }

        [Fact]
        public void Log_WithNullCategory_ShouldUseDefaultCategory()
        {
            // Arrange
            var registry = new LogRegistry();
            var client = new LoggingClient(registry);

            // Act
            client.LogInformation("Test message", category: null);

            // Assert
            var logs = registry.GetAllLogs();
            logs[0].Category.Should().Be("General");
        }

        [Fact]
        public void Log_WithEmptyCategory_ShouldUseDefaultCategory()
        {
            // Arrange
            var registry = new LogRegistry();
            var client = new LoggingClient(registry);

            // Act
            client.LogInformation("Test message", category: string.Empty);

            // Assert
            var logs = registry.GetAllLogs();
            logs[0].Category.Should().Be("General");
        }
    }
}

