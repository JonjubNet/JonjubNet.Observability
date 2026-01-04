using FluentAssertions;
using JonjubNet.Observability.Tracing.Core;
using JonjubNet.Observability.Tracing.OpenTelemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace JonjubNet.Observability.Tracing.OpenTelemetry.Tests
{
    public class OTLTraceExporterTests
    {
        [Fact]
        public void Name_ShouldReturnOpenTelemetry()
        {
            // Arrange
            var options = Options.Create(new OTLTraceOptions { Enabled = true });
            var exporter = new OTLTraceExporter(options);

            // Assert
            exporter.Name.Should().Be("OpenTelemetry");
        }

        [Fact]
        public void IsEnabled_WhenEnabled_ShouldReturnTrue()
        {
            // Arrange
            var options = Options.Create(new OTLTraceOptions { Enabled = true });
            var exporter = new OTLTraceExporter(options);

            // Assert
            exporter.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void IsEnabled_WhenDisabled_ShouldReturnFalse()
        {
            // Arrange
            var options = Options.Create(new OTLTraceOptions { Enabled = false });
            var exporter = new OTLTraceExporter(options);

            // Assert
            exporter.IsEnabled.Should().BeFalse();
        }

        [Fact]
        public async Task ExportFromRegistryAsync_WhenDisabled_ShouldNotExport()
        {
            // Arrange
            var registry = new TraceRegistry();
            var options = Options.Create(new OTLTraceOptions { Enabled = false });
            var exporter = new OTLTraceExporter(options);
            registry.AddSpan(new Span { SpanId = "span1", TraceId = "trace1", OperationName = "Test" });

            // Act
            await exporter.ExportFromRegistryAsync(registry);

            // Assert
            registry.Count.Should().Be(1); // No se limpió porque no se exportó
        }

        [Fact]
        public async Task ExportFromRegistryAsync_WhenNoHttpClient_ShouldNotExport()
        {
            // Arrange
            var registry = new TraceRegistry();
            var options = Options.Create(new OTLTraceOptions { Enabled = true, Endpoint = "http://localhost:4318" });
            var exporter = new OTLTraceExporter(options, httpClient: null);
            registry.AddSpan(new Span { SpanId = "span1", TraceId = "trace1", OperationName = "Test" });

            // Act
            await exporter.ExportFromRegistryAsync(registry);

            // Assert
            registry.Count.Should().Be(1); // No se limpió porque no hay HttpClient
        }
    }
}

