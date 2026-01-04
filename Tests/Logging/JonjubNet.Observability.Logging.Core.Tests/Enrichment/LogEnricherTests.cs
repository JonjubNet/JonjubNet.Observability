using FluentAssertions;
using JonjubNet.Observability.Logging.Core;
using JonjubNet.Observability.Logging.Core.Enrichment;
using Xunit;
using CoreLogLevel = JonjubNet.Observability.Logging.Core.LogLevel;

namespace JonjubNet.Observability.Logging.Core.Tests.Enrichment
{
    public class LogEnricherTests
    {
        [Fact]
        public void Enrich_WithEnvironment_ShouldAddEnvironmentProperty()
        {
            // Arrange
            var options = new EnrichmentOptions 
            { 
                IncludeEnvironment = true, 
                Environment = "Production" 
            };
            var enricher = new LogEnricher(options);
            var log = new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Test" };

            // Act
            enricher.Enrich(log);

            // Assert
            log.Properties.Should().ContainKey("Environment");
            log.Properties["Environment"].Should().Be("Production");
        }

        [Fact]
        public void Enrich_WithVersion_ShouldAddVersionProperty()
        {
            // Arrange
            var options = new EnrichmentOptions 
            { 
                IncludeVersion = true, 
                Version = "1.0.0" 
            };
            var enricher = new LogEnricher(options);
            var log = new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Test" };

            // Act
            enricher.Enrich(log);

            // Assert
            log.Properties.Should().ContainKey("Version");
            log.Properties["Version"].Should().Be("1.0.0");
        }

        [Fact]
        public void Enrich_WithServiceName_ShouldAddServiceNameProperty()
        {
            // Arrange
            var options = new EnrichmentOptions 
            { 
                IncludeServiceName = true, 
                ServiceName = "TestService" 
            };
            var enricher = new LogEnricher(options);
            var log = new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Test" };

            // Act
            enricher.Enrich(log);

            // Assert
            log.Properties.Should().ContainKey("ServiceName");
            log.Properties["ServiceName"].Should().Be("TestService");
        }

        [Fact]
        public void Enrich_WithMachineName_ShouldAddMachineNameProperty()
        {
            // Arrange
            var options = new EnrichmentOptions { IncludeMachineName = true };
            var enricher = new LogEnricher(options);
            var log = new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Test" };

            // Act
            enricher.Enrich(log);

            // Assert
            log.Properties.Should().ContainKey("MachineName");
            log.Properties["MachineName"].Should().Be(Environment.MachineName);
        }

        [Fact]
        public void Enrich_WithProcessInfo_ShouldAddProcessProperties()
        {
            // Arrange
            var options = new EnrichmentOptions { IncludeProcessInfo = true };
            var enricher = new LogEnricher(options);
            var log = new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Test" };

            // Act
            enricher.Enrich(log);

            // Assert
            log.Properties.Should().ContainKey("ProcessId");
            log.Properties.Should().ContainKey("ProcessName");
        }

        [Fact]
        public void Enrich_WithThreadInfo_ShouldAddThreadProperties()
        {
            // Arrange
            var options = new EnrichmentOptions { IncludeThreadInfo = true };
            var enricher = new LogEnricher(options);
            var log = new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Test" };

            // Act
            enricher.Enrich(log);

            // Assert
            log.Properties.Should().ContainKey("ThreadId");
            log.Properties.Should().ContainKey("ThreadName");
        }

        [Fact]
        public void Enrich_WithCustomProperties_ShouldAddCustomProperties()
        {
            // Arrange
            var options = new EnrichmentOptions 
            { 
                CustomProperties = new Dictionary<string, object?> 
                { 
                    ["custom1"] = "value1", 
                    ["custom2"] = 42 
                } 
            };
            var enricher = new LogEnricher(options);
            var log = new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Test" };

            // Act
            enricher.Enrich(log);

            // Assert
            log.Properties.Should().ContainKey("custom1");
            log.Properties.Should().ContainKey("custom2");
            log.Properties["custom1"].Should().Be("value1");
            log.Properties["custom2"].Should().Be(42);
        }

        [Fact]
        public void Enrich_WithCustomTags_ShouldAddCustomTags()
        {
            // Arrange
            var options = new EnrichmentOptions 
            { 
                CustomTags = new Dictionary<string, string> 
                { 
                    ["tag1"] = "value1" 
                } 
            };
            var enricher = new LogEnricher(options);
            var log = new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Test" };

            // Act
            enricher.Enrich(log);

            // Assert
            log.Tags.Should().ContainKey("tag1");
            log.Tags["tag1"].Should().Be("value1");
        }

        [Fact]
        public void Enrich_WithUserId_ShouldSetUserId()
        {
            // Arrange
            var options = new EnrichmentOptions 
            { 
                IncludeUserInfo = true, 
                UserId = "user123" 
            };
            var enricher = new LogEnricher(options);
            var log = new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Test" };

            // Act
            enricher.Enrich(log);

            // Assert
            log.UserId.Should().Be("user123");
        }

        [Fact]
        public void Enrich_WithCorrelationId_ShouldSetCorrelationId()
        {
            // Arrange
            var options = new EnrichmentOptions 
            { 
                IncludeCorrelationId = true, 
                CorrelationId = "corr123" 
            };
            var enricher = new LogEnricher(options);
            var log = new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Test" };

            // Act
            enricher.Enrich(log);

            // Assert
            log.CorrelationId.Should().Be("corr123");
        }
    }
}

