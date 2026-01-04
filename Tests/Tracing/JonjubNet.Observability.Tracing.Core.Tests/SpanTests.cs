using FluentAssertions;
using JonjubNet.Observability.Tracing.Core;
using Xunit;

namespace JonjubNet.Observability.Tracing.Core.Tests
{
    public class SpanTests
    {
        [Fact]
        public void Span_ShouldInitializeWithDefaults()
        {
            // Act
            var span = new Span();

            // Assert
            span.SpanId.Should().BeEmpty();
            span.TraceId.Should().BeEmpty();
            span.OperationName.Should().BeEmpty();
            span.Kind.Should().Be(SpanKind.Internal);
            span.Status.Should().Be(SpanStatus.Unset);
            span.Tags.Should().NotBeNull();
            span.Events.Should().NotBeNull();
            span.Properties.Should().NotBeNull();
            span.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Span_WithAllProperties_ShouldSetAllProperties()
        {
            // Arrange
            var startTime = DateTimeOffset.UtcNow;
            var endTime = startTime.AddMilliseconds(100);

            // Act
            var span = new Span
            {
                SpanId = "span123",
                TraceId = "trace123",
                ParentSpanId = "parent123",
                OperationName = "TestOperation",
                Kind = SpanKind.Server,
                Status = SpanStatus.Ok,
                StartTime = startTime,
                EndTime = endTime,
                DurationMs = 100,
                Tags = new Dictionary<string, string> { ["env"] = "prod" },
                Events = new List<SpanEvent> { new SpanEvent { Name = "event1" } },
                Properties = new Dictionary<string, object?> { ["prop1"] = "value1" }
            };

            // Assert
            span.SpanId.Should().Be("span123");
            span.TraceId.Should().Be("trace123");
            span.ParentSpanId.Should().Be("parent123");
            span.OperationName.Should().Be("TestOperation");
            span.Kind.Should().Be(SpanKind.Server);
            span.Status.Should().Be(SpanStatus.Ok);
            span.StartTime.Should().Be(startTime);
            span.EndTime.Should().Be(endTime);
            span.DurationMs.Should().Be(100);
            span.Tags.Should().ContainKey("env");
            span.Events.Should().HaveCount(1);
            span.Properties.Should().ContainKey("prop1");
        }

        [Fact]
        public void SpanEvent_ShouldInitializeWithDefaults()
        {
            // Act
            var spanEvent = new SpanEvent();

            // Assert
            spanEvent.Name.Should().BeEmpty();
            spanEvent.Attributes.Should().NotBeNull();
        }

        [Fact]
        public void SpanEvent_WithAllProperties_ShouldSetAllProperties()
        {
            // Arrange
            var timestamp = DateTimeOffset.UtcNow;

            // Act
            var spanEvent = new SpanEvent
            {
                Name = "TestEvent",
                Timestamp = timestamp,
                Attributes = new Dictionary<string, object?> { ["attr1"] = "value1" }
            };

            // Assert
            spanEvent.Name.Should().Be("TestEvent");
            spanEvent.Timestamp.Should().Be(timestamp);
            spanEvent.Attributes.Should().ContainKey("attr1");
        }
    }
}

