using FluentAssertions;
using JonjubNet.Observability.Tracing.Core;
using JonjubNet.Observability.Tracing.Core.Interfaces;
using Xunit;

namespace JonjubNet.Observability.Tracing.Integration.Tests
{
    public class TracingIntegrationTests
    {
        [Fact]
        public void TracingClient_WithRegistry_ShouldWorkEndToEnd()
        {
            // Arrange
            var registry = new TraceRegistry();
            var client = new TracingClient(registry);

            // Act
            var span = client.StartSpan("TestOperation", SpanKind.Internal);
            span.SetTag("env", "test");
            span.Finish();

            // Assert
            registry.Count.Should().Be(1);
            var spans = registry.GetAllSpans();
            spans.Should().HaveCount(1);
            spans[0].OperationName.Should().Be("TestOperation");
            spans[0].Tags.Should().ContainKey("env");
        }

        [Fact]
        public void TracingClient_WithChildSpan_ShouldCreateParentChildRelationship()
        {
            // Arrange
            var registry = new TraceRegistry();
            var client = new TracingClient(registry);

            // Act
            var parentSpan = client.StartSpan("ParentOperation");
            var childSpan = client.StartChildSpan("ChildOperation");
            childSpan.Finish();
            parentSpan.Finish();

            // Assert
            registry.Count.Should().Be(2);
            var spans = registry.GetAllSpans();
            var child = spans.First(s => s.OperationName == "ChildOperation");
            child.ParentSpanId.Should().Be(parentSpan.SpanId);
            child.TraceId.Should().Be(parentSpan.TraceId);
        }

        [Fact]
        public void TracingClient_WithScope_ShouldCreateScope()
        {
            // Arrange
            var registry = new TraceRegistry();
            var client = new TracingClient(registry);

            // Act
            using (client.BeginScope("TestScope", new Dictionary<string, object?> { ["scopeProp"] = "scopeValue" }))
            {
                var span = client.StartSpan("OperationInScope");
                span.Finish();
            }

            // Assert
            registry.Count.Should().BeGreaterThan(0);
        }

        [Fact]
        public void TracingClient_BeginOperation_ShouldAutoFinish()
        {
            // Arrange
            var registry = new TraceRegistry();
            var client = new TracingClient(registry);

            // Act
            using (client.BeginOperation("TestOperation"))
            {
                // Operation is active
            }

            // Assert
            registry.Count.Should().Be(1);
            var spans = registry.GetAllSpans();
            spans[0].OperationName.Should().Be("TestOperation");
            spans[0].IsActive.Should().BeFalse();
        }

        [Fact]
        public void TracingClient_WithException_ShouldRecordException()
        {
            // Arrange
            var registry = new TraceRegistry();
            var client = new TracingClient(registry);
            var exception = new InvalidOperationException("Test exception");

            // Act
            var span = client.StartSpan("FailingOperation");
            span.RecordException(exception);
            span.Finish();

            // Assert
            var spans = registry.GetAllSpans();
            spans[0].Status.Should().Be(SpanStatus.Error);
            spans[0].ErrorMessage.Should().Be("Test exception");
            spans[0].Events.Should().Contain(e => e.Name == "exception");
        }
    }
}

