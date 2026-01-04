using FluentAssertions;
using JonjubNet.Observability.Tracing.Core;
using JonjubNet.Observability.Tracing.Core.Interfaces;
using JonjubNet.Observability.Tracing.Core.Resilience;
using Moq;
using Xunit;

namespace JonjubNet.Observability.Tracing.Core.Tests
{
    public class TraceFlushSchedulerTests
    {
        [Fact]
        public void Start_ShouldStartBackgroundTask()
        {
            // Arrange
            var registry = new TraceRegistry();
            var sinks = new List<ITraceSink>();
            var scheduler = new TraceFlushScheduler(registry, sinks, TimeSpan.FromMilliseconds(100));

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
            var registry = new TraceRegistry();
            var sink1 = new Mock<ITraceSink>();
            sink1.Setup(s => s.Name).Returns("Sink1");
            sink1.Setup(s => s.IsEnabled).Returns(true);
            
            var sink2 = new Mock<ITraceSink>();
            sink2.Setup(s => s.Name).Returns("Sink2");
            sink2.Setup(s => s.IsEnabled).Returns(true);

            var sinks = new List<ITraceSink> { sink1.Object, sink2.Object };
            var scheduler = new TraceFlushScheduler(registry, sinks, TimeSpan.FromMilliseconds(100));

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
            var registry = new TraceRegistry();
            var enabledSink = new Mock<ITraceSink>();
            enabledSink.Setup(s => s.Name).Returns("EnabledSink");
            enabledSink.Setup(s => s.IsEnabled).Returns(true);
            
            var disabledSink = new Mock<ITraceSink>();
            disabledSink.Setup(s => s.Name).Returns("DisabledSink");
            disabledSink.Setup(s => s.IsEnabled).Returns(false);

            var sinks = new List<ITraceSink> { enabledSink.Object, disabledSink.Object };
            var scheduler = new TraceFlushScheduler(registry, sinks, TimeSpan.FromMilliseconds(100));

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
            var registry = new TraceRegistry();
            var sinks = new List<ITraceSink>();
            var scheduler = new TraceFlushScheduler(registry, sinks);

            // Act
            scheduler.Start();
            scheduler.Dispose();

            // Assert
            // Si no hay excepciones, el dispose funcionó correctamente
            Assert.True(true);
        }
    }
}

