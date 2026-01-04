using FluentAssertions;
using JonjubNet.Observability.Logging.Core;
using JonjubNet.Observability.Logging.Kafka;
using JonjubNet.Observability.Shared.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace JonjubNet.Observability.Logging.Kafka.Tests
{
    public class KafkaLogSinkTests
    {
        [Fact]
        public void Name_ShouldReturnKafka()
        {
            // Arrange
            var options = Options.Create(new KafkaOptions { Enabled = true });
            var factory = new KafkaProducerFactory(Mock.Of<ILogger<KafkaProducerFactory>>());
            var sink = new KafkaLogSink(options, factory);

            // Assert
            sink.Name.Should().Be("Kafka");
        }

        [Fact]
        public void IsEnabled_WhenEnabled_ShouldReturnTrue()
        {
            // Arrange
            var options = Options.Create(new KafkaOptions { Enabled = true });
            var factory = new KafkaProducerFactory(Mock.Of<ILogger<KafkaProducerFactory>>());
            var sink = new KafkaLogSink(options, factory);

            // Assert
            sink.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void IsEnabled_WhenDisabled_ShouldReturnFalse()
        {
            // Arrange
            var options = Options.Create(new KafkaOptions { Enabled = false });
            var factory = new KafkaProducerFactory(Mock.Of<ILogger<KafkaProducerFactory>>());
            var sink = new KafkaLogSink(options, factory);

            // Assert
            sink.IsEnabled.Should().BeFalse();
        }
    }
}

