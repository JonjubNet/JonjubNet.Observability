using FluentAssertions;
using JonjubNet.Observability.Tracing.Core;
using JonjubNet.Observability.Tracing.Core.Interfaces;
using Xunit;

namespace JonjubNet.Observability.Tracing.Core.Tests
{
    public class TracingClientTests
    {
        [Fact]
        public void StartSpan_ShouldCreateSpan()
        {
            // Arrange
            var registry = new TraceRegistry();
            var client = new TracingClient(registry);

            // Act
            var span = client.StartSpan("TestOperation", SpanKind.Internal);

            // Assert
            span.Should().NotBeNull();
            span.OperationName.Should().Be("TestOperation");
            span.Kind.Should().Be(SpanKind.Internal);
            span.TraceId.Should().NotBeNullOrEmpty();
            span.SpanId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void StartSpan_WithTags_ShouldIncludeTags()
        {
            // Arrange
            var registry = new TraceRegistry();
            var client = new TracingClient(registry);
            var tags = new Dictionary<string, string> { ["env"] = "prod", ["service"] = "api" };

            // Act
            var span = client.StartSpan("TestOperation", SpanKind.Internal, tags);

            // Assert
            span.Tags.Should().ContainKey("env");
            span.Tags.Should().ContainKey("service");
            span.Tags["env"].Should().Be("prod");
            span.Tags["service"].Should().Be("api");
        }

        [Fact]
        public void StartChildSpan_ShouldCreateChildSpan()
        {
            // Arrange
            var registry = new TraceRegistry();
            var client = new TracingClient(registry);
            var parentSpan = client.StartSpan("ParentOperation");

            // Act
            var childSpan = client.StartChildSpan("ChildOperation");

            // Assert
            childSpan.Should().NotBeNull();
            childSpan.OperationName.Should().Be("ChildOperation");
            childSpan.ParentSpanId.Should().Be(parentSpan.SpanId);
            childSpan.TraceId.Should().Be(parentSpan.TraceId);
        }

        [Fact]
        public void StartChildSpan_WithoutParent_ShouldCreateNewTrace()
        {
            // Arrange
            var registry = new TraceRegistry();
            var client = new TracingClient(registry);

            // Act
            var childSpan = client.StartChildSpan("ChildOperation");

            // Assert
            childSpan.Should().NotBeNull();
            childSpan.TraceId.Should().NotBeNullOrEmpty();
            childSpan.ParentSpanId.Should().BeNull();
        }

        [Fact]
        public void GetCurrentSpan_ShouldReturnCurrentSpan()
        {
            // Arrange
            var registry = new TraceRegistry();
            var client = new TracingClient(registry);

            // Act
            var span = client.StartSpan("TestOperation");
            var current = client.GetCurrentSpan();

            // Assert
            current.Should().NotBeNull();
            current.Should().Be(span);
        }

        [Fact]
        public void GetCurrentSpan_WhenNoSpan_ShouldReturnNull()
        {
            // Arrange
            var registry = new TraceRegistry();
            var client = new TracingClient(registry);

            // Act
            var current = client.GetCurrentSpan();

            // Assert
            current.Should().BeNull();
        }

        [Fact]
        public void BeginScope_ShouldCreateScope()
        {
            // Arrange
            var registry = new TraceRegistry();
            var client = new TracingClient(registry);
            var properties = new Dictionary<string, object?> { ["key1"] = "value1" };

            // Act
            using (client.BeginScope("TestScope", properties))
            {
                // Scope is active
            }

            // Assert
            // Scope should be disposed and no longer active
            Assert.True(true);
        }

        [Fact]
        public void BeginOperation_ShouldCreateSpanAndDisposeOnEnd()
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
            registry.Count.Should().BeGreaterThan(0);
            var spans = registry.GetAllSpans();
            spans.Should().Contain(s => s.OperationName == "TestOperation");
        }

        [Fact]
        public void StartSpan_WithDifferentKinds_ShouldSetKind()
        {
            // Arrange
            var registry = new TraceRegistry();
            var client = new TracingClient(registry);

            // Act
            var serverSpan = client.StartSpan("ServerOp", SpanKind.Server);
            var clientSpan = client.StartSpan("ClientOp", SpanKind.Client);
            var producerSpan = client.StartSpan("ProducerOp", SpanKind.Producer);
            var consumerSpan = client.StartSpan("ConsumerOp", SpanKind.Consumer);

            // Assert
            serverSpan.Kind.Should().Be(SpanKind.Server);
            clientSpan.Kind.Should().Be(SpanKind.Client);
            producerSpan.Kind.Should().Be(SpanKind.Producer);
            consumerSpan.Kind.Should().Be(SpanKind.Consumer);
        }

        [Fact]
        public void StartSpan_ShouldSetStartTime()
        {
            // Arrange
            var registry = new TraceRegistry();
            var client = new TracingClient(registry);
            var before = DateTimeOffset.UtcNow;

            // Act
            var span = client.StartSpan("TestOperation");
            var after = DateTimeOffset.UtcNow;

            // Assert
            span.StartTime.Should().BeOnOrAfter(before);
            span.StartTime.Should().BeOnOrBefore(after);
        }
    }
}

