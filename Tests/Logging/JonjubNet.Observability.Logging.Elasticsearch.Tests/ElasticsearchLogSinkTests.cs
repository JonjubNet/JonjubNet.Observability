using FluentAssertions;
using JonjubNet.Observability.Logging.Core;
using JonjubNet.Observability.Logging.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace JonjubNet.Observability.Logging.Elasticsearch.Tests
{
    public class ElasticsearchLogSinkTests
    {
        [Fact]
        public void Name_ShouldReturnElasticsearch()
        {
            // Arrange
            var options = Options.Create(new ElasticsearchOptions { Enabled = true });
            var sink = new ElasticsearchLogSink(options);

            // Assert
            sink.Name.Should().Be("Elasticsearch");
        }

        [Fact]
        public void IsEnabled_WhenEnabled_ShouldReturnTrue()
        {
            // Arrange
            var options = Options.Create(new ElasticsearchOptions { Enabled = true });
            var sink = new ElasticsearchLogSink(options);

            // Assert
            sink.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void IsEnabled_WhenDisabled_ShouldReturnFalse()
        {
            // Arrange
            var options = Options.Create(new ElasticsearchOptions { Enabled = false });
            var sink = new ElasticsearchLogSink(options);

            // Assert
            sink.IsEnabled.Should().BeFalse();
        }
    }
}

