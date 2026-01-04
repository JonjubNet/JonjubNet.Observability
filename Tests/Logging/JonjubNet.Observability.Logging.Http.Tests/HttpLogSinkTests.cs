using FluentAssertions;
using JonjubNet.Observability.Logging.Core;
using JonjubNet.Observability.Logging.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace JonjubNet.Observability.Logging.Http.Tests
{
    public class HttpLogSinkTests
    {
        [Fact]
        public void Name_ShouldReturnHttp()
        {
            // Arrange
            var options = Options.Create(new HttpOptions { Enabled = true });
            var sink = new HttpLogSink(options);

            // Assert
            sink.Name.Should().Be("Http");
        }

        [Fact]
        public void IsEnabled_WhenEnabled_ShouldReturnTrue()
        {
            // Arrange
            var options = Options.Create(new HttpOptions { Enabled = true });
            var sink = new HttpLogSink(options);

            // Assert
            sink.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void IsEnabled_WhenDisabled_ShouldReturnFalse()
        {
            // Arrange
            var options = Options.Create(new HttpOptions { Enabled = false });
            var sink = new HttpLogSink(options);

            // Assert
            sink.IsEnabled.Should().BeFalse();
        }
    }
}

