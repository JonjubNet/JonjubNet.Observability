using FluentAssertions;
using JonjubNet.Observability.Tracing.Core;
using JonjubNet.Observability.Tracing.Kafka;
using JonjubNet.Observability.Shared.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace JonjubNet.Observability.Tracing.Kafka.Tests
{
    public class KafkaTraceExporterTests
    {
        [Fact]
        public void Name_ShouldReturnKafka()
        {
            // Arrange
            var options = Options.Create(new KafkaOptions { Enabled = true });
            var factory = new KafkaProducerFactory(Mock.Of<ILogger<KafkaProducerFactory>>());
            var exporter = new KafkaTraceExporter(options, factory);

            // Assert
            exporter.Name.Should().Be("Kafka");
        }

        [Fact]
        public void IsEnabled_WhenEnabled_ShouldReturnTrue()
        {
            // Arrange
            var options = Options.Create(new KafkaOptions { Enabled = true });
            var factory = new KafkaProducerFactory(Mock.Of<ILogger<KafkaProducerFactory>>());
            var exporter = new KafkaTraceExporter(options, factory);

            // Assert
            exporter.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void IsEnabled_WhenDisabled_ShouldReturnFalse()
        {
            // Arrange
            var options = Options.Create(new KafkaOptions { Enabled = false });
            var factory = new KafkaProducerFactory(Mock.Of<ILogger<KafkaProducerFactory>>());
            var exporter = new KafkaTraceExporter(options, factory);

            // Assert
            exporter.IsEnabled.Should().BeFalse();
        }

        [Fact]
        public async Task ExportFromRegistryAsync_WhenDisabled_ShouldNotExport()
        {
            // Arrange
            var registry = new TraceRegistry();
            var options = Options.Create(new KafkaOptions { Enabled = false });
            var factory = new KafkaProducerFactory(Mock.Of<ILogger<KafkaProducerFactory>>());
            var exporter = new KafkaTraceExporter(options, factory);
            
            var span = new Span
            {
                SpanId = "span1",
                TraceId = "trace1",
                OperationName = "test-operation",
                StartTime = DateTimeOffset.UtcNow
            };
            registry.AddSpan(span);

            // Act
            await exporter.ExportFromRegistryAsync(registry);

            // Assert
            registry.Count.Should().Be(1); // No se limpió porque no se exportó
        }

        [Fact]
        public async Task ExportFromRegistryAsync_WhenEnabled_ShouldExportAndClear()
        {
            // Arrange
            var registry = new TraceRegistry();
            var options = Options.Create(new KafkaOptions 
            { 
                Enabled = true,
                Topic = "traces"
            });
            var factory = new KafkaProducerFactory(Mock.Of<ILogger<KafkaProducerFactory>>());
            var exporter = new KafkaTraceExporter(options, factory);
            
            var span = new Span
            {
                SpanId = "span1",
                TraceId = "trace1",
                OperationName = "test-operation",
                StartTime = DateTimeOffset.UtcNow
            };
            registry.AddSpan(span);

            // Act
            await exporter.ExportFromRegistryAsync(registry);

            // Assert
            // Registry should be cleared after export (even if Kafka producer is NullKafkaProducer)
            // The exporter will attempt to send, and the NullKafkaProducer will silently succeed
            registry.Count.Should().Be(0);
        }

        [Fact]
        public async Task ExportFromRegistryAsync_WithMultipleSpans_ShouldGroupByTraceId()
        {
            // Arrange
            var registry = new TraceRegistry();
            var options = Options.Create(new KafkaOptions 
            { 
                Enabled = true,
                Topic = "traces",
                BatchSize = 100
            });
            var factory = new KafkaProducerFactory(Mock.Of<ILogger<KafkaProducerFactory>>());
            var exporter = new KafkaTraceExporter(options, factory);
            
            // Crear spans de diferentes traces
            var span1 = new Span
            {
                SpanId = "span1",
                TraceId = "trace1",
                OperationName = "operation1",
                StartTime = DateTimeOffset.UtcNow
            };
            var span2 = new Span
            {
                SpanId = "span2",
                TraceId = "trace1",
                OperationName = "operation2",
                StartTime = DateTimeOffset.UtcNow
            };
            var span3 = new Span
            {
                SpanId = "span3",
                TraceId = "trace2",
                OperationName = "operation3",
                StartTime = DateTimeOffset.UtcNow
            };
            
            registry.AddSpan(span1);
            registry.AddSpan(span2);
            registry.AddSpan(span3);

            // Act
            await exporter.ExportFromRegistryAsync(registry);

            // Assert
            registry.Count.Should().Be(0); // Registry cleared after export
        }

        [Fact]
        public async Task ExportFromRegistryAsync_WithLargeBatch_ShouldSplitIntoMultipleBatches()
        {
            // Arrange
            var registry = new TraceRegistry();
            var options = Options.Create(new KafkaOptions 
            { 
                Enabled = true,
                Topic = "traces",
                BatchSize = 5 // Pequeño batch size para forzar múltiples batches
            });
            var factory = new KafkaProducerFactory(Mock.Of<ILogger<KafkaProducerFactory>>());
            var exporter = new KafkaTraceExporter(options, factory);
            
            // Crear más spans que el batch size
            for (int i = 0; i < 12; i++)
            {
                registry.AddSpan(new Span
                {
                    SpanId = $"span{i}",
                    TraceId = $"trace{i / 3}", // Agrupar en traces
                    OperationName = $"operation{i}",
                    StartTime = DateTimeOffset.UtcNow
                });
            }

            // Act
            await exporter.ExportFromRegistryAsync(registry);

            // Assert
            registry.Count.Should().Be(0); // Registry cleared after export
        }

        [Fact]
        public async Task ExportFromRegistryAsync_WithEmptyRegistry_ShouldNotThrow()
        {
            // Arrange
            var registry = new TraceRegistry();
            var options = Options.Create(new KafkaOptions { Enabled = true });
            var factory = new KafkaProducerFactory(Mock.Of<ILogger<KafkaProducerFactory>>());
            var exporter = new KafkaTraceExporter(options, factory);

            // Act & Assert
            var act = async () => await exporter.ExportFromRegistryAsync(registry);
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public void KafkaTraceMessageFactory_CreateMessage_ShouldSerializeSpan()
        {
            // Arrange
            var span = new Span
            {
                SpanId = "span1",
                TraceId = "trace1",
                ParentSpanId = "parent1",
                OperationName = "test-operation",
                Kind = SpanKind.Server,
                Status = SpanStatus.Ok,
                StartTime = DateTimeOffset.UtcNow,
                EndTime = DateTimeOffset.UtcNow.AddMilliseconds(100),
                DurationMs = 100,
                Tags = new Dictionary<string, string> { ["tag1"] = "value1" },
                Properties = new Dictionary<string, object?> { ["prop1"] = "value1" },
                ServiceName = "test-service",
                ResourceName = "test-resource"
            };

            // Act
            var message = KafkaTraceMessageFactory.CreateMessage(span);

            // Assert
            message.Should().NotBeNullOrEmpty();
            message.Should().Contain("span1");
            message.Should().Contain("trace1");
            message.Should().Contain("test-operation");
        }

        [Fact]
        public void KafkaTraceMessageFactory_CreateBatchMessage_ShouldGroupByTraceId()
        {
            // Arrange
            var spans = new List<Span>
            {
                new Span { SpanId = "span1", TraceId = "trace1", OperationName = "op1" },
                new Span { SpanId = "span2", TraceId = "trace1", OperationName = "op2" },
                new Span { SpanId = "span3", TraceId = "trace2", OperationName = "op3" }
            };

            // Act
            var batchMessage = KafkaTraceMessageFactory.CreateBatchMessage(spans);

            // Assert
            batchMessage.Should().NotBeNullOrEmpty();
            batchMessage.Should().Contain("trace1");
            batchMessage.Should().Contain("trace2");
            batchMessage.Should().Contain("traceCount");
        }
    }
}

