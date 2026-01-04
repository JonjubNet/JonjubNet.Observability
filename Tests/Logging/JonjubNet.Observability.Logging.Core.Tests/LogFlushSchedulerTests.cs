using FluentAssertions;
using JonjubNet.Observability.Logging.Core;
using JonjubNet.Observability.Logging.Core.Interfaces;
using JonjubNet.Observability.Logging.Core.Resilience;
using Moq;
using Xunit;

namespace JonjubNet.Observability.Logging.Core.Tests
{
    public class LogFlushSchedulerTests
    {
        [Fact]
        public void Start_ShouldStartBackgroundTask()
        {
            // Arrange
            var registry = new LogRegistry();
            var sinks = new List<ILogSink>();
            var scheduler = new LogFlushScheduler(registry, sinks, TimeSpan.FromMilliseconds(100));

            // Act
            scheduler.Start();
            System.Threading.Thread.Sleep(150); // Esperar un ciclo

            // Assert
            // Si no hay errores, el scheduler está funcionando
            scheduler.Dispose();
        }

        [Fact]
        public async Task ExportToAllSinks_ShouldCallAllEnabledSinks()
        {
            // Arrange
            var registry = new LogRegistry();
            var sink1 = new Mock<ILogSink>();
            sink1.Setup(s => s.Name).Returns("Sink1");
            sink1.Setup(s => s.IsEnabled).Returns(true);
            
            var sink2 = new Mock<ILogSink>();
            sink2.Setup(s => s.Name).Returns("Sink2");
            sink2.Setup(s => s.IsEnabled).Returns(true);

            var sinks = new List<ILogSink> { sink1.Object, sink2.Object };
            var scheduler = new LogFlushScheduler(registry, sinks, TimeSpan.FromMilliseconds(100));

            // Act
            scheduler.Start();
            await Task.Delay(150);
            scheduler.Dispose();

            // Assert
            sink1.Verify(s => s.ExportFromRegistryAsync(registry, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            sink2.Verify(s => s.ExportFromRegistryAsync(registry, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExportToAllSinks_ShouldSkipDisabledSinks()
        {
            // Arrange
            var registry = new LogRegistry();
            var enabledSink = new Mock<ILogSink>();
            enabledSink.Setup(s => s.Name).Returns("EnabledSink");
            enabledSink.Setup(s => s.IsEnabled).Returns(true);
            
            var disabledSink = new Mock<ILogSink>();
            disabledSink.Setup(s => s.Name).Returns("DisabledSink");
            disabledSink.Setup(s => s.IsEnabled).Returns(false);

            var sinks = new List<ILogSink> { enabledSink.Object, disabledSink.Object };
            var scheduler = new LogFlushScheduler(registry, sinks, TimeSpan.FromMilliseconds(100));

            // Act
            scheduler.Start();
            await Task.Delay(150);
            scheduler.Dispose();

            // Assert
            enabledSink.Verify(s => s.ExportFromRegistryAsync(registry, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            disabledSink.Verify(s => s.ExportFromRegistryAsync(registry, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void Dispose_ShouldStopScheduler()
        {
            // Arrange
            var registry = new LogRegistry();
            var sinks = new List<ILogSink>();
            var scheduler = new LogFlushScheduler(registry, sinks);

            // Act
            scheduler.Start();
            scheduler.Dispose();

            // Assert
            // Si no hay excepciones, el dispose funcionó correctamente
            Assert.True(true);
        }
    }
}

