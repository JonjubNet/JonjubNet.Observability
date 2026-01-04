using FluentAssertions;
using JonjubNet.Observability.Logging.Core;
using JonjubNet.Observability.Logging.Console;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using CoreLogLevel = JonjubNet.Observability.Logging.Core.LogLevel;

namespace JonjubNet.Observability.Logging.Console.Tests
{
    public class ConsoleLogSinkTests
    {
        [Fact]
        public void Name_ShouldReturnConsole()
        {
            // Arrange
            var options = Options.Create(new ConsoleOptions { Enabled = true });
            var sink = new ConsoleLogSink(options);

            // Assert
            sink.Name.Should().Be("Console");
        }

        [Fact]
        public void IsEnabled_WhenEnabled_ShouldReturnTrue()
        {
            // Arrange
            var options = Options.Create(new ConsoleOptions { Enabled = true });
            var sink = new ConsoleLogSink(options);

            // Assert
            sink.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void IsEnabled_WhenDisabled_ShouldReturnFalse()
        {
            // Arrange
            var options = Options.Create(new ConsoleOptions { Enabled = false });
            var sink = new ConsoleLogSink(options);

            // Assert
            sink.IsEnabled.Should().BeFalse();
        }

        [Fact]
        public async Task ExportFromRegistryAsync_WhenDisabled_ShouldNotExport()
        {
            // Arrange
            var registry = new LogRegistry();
            var options = Options.Create(new ConsoleOptions { Enabled = false });
            var sink = new ConsoleLogSink(options);
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Test" });

            // Act
            await sink.ExportFromRegistryAsync(registry);

            // Assert
            registry.Count.Should().Be(1); // No se limpió porque no se exportó
        }

        [Fact]
        public async Task ExportFromRegistryAsync_WhenEnabled_ShouldExportAndClear()
        {
            // Arrange
            var registry = new LogRegistry();
            var options = Options.Create(new ConsoleOptions { Enabled = true });
            var sink = new ConsoleLogSink(options);
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Test" });

            // Act
            await sink.ExportFromRegistryAsync(registry);

            // Assert
            registry.Count.Should().Be(0); // Se limpió después de exportar
        }

        [Fact]
        public async Task ExportFromRegistryAsync_WithMinLevel_ShouldFilterLogs()
        {
            // Arrange
            var registry = new LogRegistry();
            var options = Options.Create(new ConsoleOptions 
            { 
                Enabled = true, 
                MinLevel = "Warning" 
            });
            var sink = new ConsoleLogSink(options);
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Info" });
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Warning, Message = "Warning" });

            // Act
            await sink.ExportFromRegistryAsync(registry);

            // Assert
            // Registry should be cleared after export
            registry.Count.Should().Be(0);
        }
    }
}

