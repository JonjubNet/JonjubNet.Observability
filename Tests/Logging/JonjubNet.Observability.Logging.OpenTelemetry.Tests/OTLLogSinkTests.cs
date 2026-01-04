using FluentAssertions;
using JonjubNet.Observability.Logging.Core;
using JonjubNet.Observability.Logging.OpenTelemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using CoreLogLevel = JonjubNet.Observability.Logging.Core.LogLevel;

namespace JonjubNet.Observability.Logging.OpenTelemetry.Tests
{
    public class OTLLogSinkTests
    {
        [Fact]
        public void Name_ShouldReturnOpenTelemetry()
        {
            // Arrange
            var options = Options.Create(new OTLLogOptions { Enabled = true });
            var sink = new OTLLogSink(options);

            // Assert
            sink.Name.Should().Be("OpenTelemetry");
        }

        [Fact]
        public void IsEnabled_WhenEnabled_ShouldReturnTrue()
        {
            // Arrange
            var options = Options.Create(new OTLLogOptions { Enabled = true });
            var sink = new OTLLogSink(options);

            // Assert
            sink.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void IsEnabled_WhenDisabled_ShouldReturnFalse()
        {
            // Arrange
            var options = Options.Create(new OTLLogOptions { Enabled = false });
            var sink = new OTLLogSink(options);

            // Assert
            sink.IsEnabled.Should().BeFalse();
        }

        [Fact]
        public async Task ExportFromRegistryAsync_WhenDisabled_ShouldNotExport()
        {
            // Arrange
            var registry = new LogRegistry();
            var options = Options.Create(new OTLLogOptions { Enabled = false });
            var sink = new OTLLogSink(options);
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Test" });

            // Act
            await sink.ExportFromRegistryAsync(registry);

            // Assert
            registry.Count.Should().Be(1); // No se limpió porque no se exportó
        }

        [Fact]
        public async Task ExportFromRegistryAsync_WhenNoHttpClient_ShouldNotExport()
        {
            // Arrange
            var registry = new LogRegistry();
            var options = Options.Create(new OTLLogOptions { Enabled = true, Endpoint = "http://localhost:4318" });
            var sink = new OTLLogSink(options, httpClient: null);
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Test" });

            // Act
            await sink.ExportFromRegistryAsync(registry);

            // Assert
            registry.Count.Should().Be(1); // No se limpió porque no hay HttpClient
        }
    }
}

