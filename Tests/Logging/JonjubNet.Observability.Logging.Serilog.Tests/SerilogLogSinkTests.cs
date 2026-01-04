using FluentAssertions;
using JonjubNet.Observability.Logging.Core;
using JonjubNet.Observability.Logging.Serilog;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace JonjubNet.Observability.Logging.Serilog.Tests
{
    public class SerilogLogSinkTests
    {
        [Fact]
        public void Name_ShouldReturnSerilog()
        {
            // Arrange
            var options = Options.Create(new SerilogOptions { Enabled = true });
            var sink = new SerilogLogSink(options);

            // Assert
            sink.Name.Should().Be("Serilog");
        }

        [Fact]
        public void IsEnabled_WhenEnabled_ShouldReturnTrue()
        {
            // Arrange
            var options = Options.Create(new SerilogOptions { Enabled = true });
            var sink = new SerilogLogSink(options);

            // Assert
            sink.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void IsEnabled_WhenDisabled_ShouldReturnFalse()
        {
            // Arrange
            var options = Options.Create(new SerilogOptions { Enabled = false });
            var sink = new SerilogLogSink(options);

            // Assert
            sink.IsEnabled.Should().BeFalse();
        }
    }
}

