using FluentAssertions;
using JonjubNet.Observability.Tracing.Core;
using JonjubNet.Observability.Tracing.Core.Interfaces;
using Xunit;

namespace JonjubNet.Observability.Tracing.Core.Tests
{
    public class SpanImplTests
    {
        [Fact]
        public void SetTag_ShouldAddTag()
        {
            // Arrange
            var registry = new TraceRegistry();
            var client = new TracingClient(registry);
            var span = client.StartSpan("Test", SpanKind.Internal);

            // Act
            span.SetTag("env", "prod");

            // Assert
            span.Tags.Should().ContainKey("env");
            span.Tags["env"].Should().Be("prod");
        }

        [Fact]
        public void SetTags_ShouldAddMultipleTags()
        {
            // Arrange
            var registry = new TraceRegistry();
            var client = new TracingClient(registry);
            var span = client.StartSpan("Test", SpanKind.Internal);
            var tags = new Dictionary<string, string> { ["env"] = "prod", ["service"] = "api" };

            // Act
            span.SetTags(tags);

            // Assert
            span.Tags.Should().ContainKey("env");
            span.Tags.Should().ContainKey("service");
            span.Tags["env"].Should().Be("prod");
            span.Tags["service"].Should().Be("api");
        }

        [Fact]
        public void AddEvent_ShouldAddEvent()
        {
            // Arrange
            var registry = new TraceRegistry();
            var client = new TracingClient(registry);
            var span = client.StartSpan("Test", SpanKind.Internal);

            // Act
            span.AddEvent("TestEvent", new Dictionary<string, object?> { ["attr1"] = "value1" });

            // Assert
            span.Events.Should().HaveCount(1);
            span.Events[0].Name.Should().Be("TestEvent");
            span.Events[0].Attributes.Should().ContainKey("attr1");
        }

        [Fact]
        public void RecordException_ShouldSetErrorStatusAndAddEvent()
        {
            // Arrange
            var registry = new TraceRegistry();
            var client = new TracingClient(registry);
            var span = client.StartSpan("Test", SpanKind.Internal);
            var exception = new InvalidOperationException("Test exception");

            // Act
            span.RecordException(exception);

            // Assert
            span.Status.Should().Be(SpanStatus.Error);
            var spans = registry.GetAllSpans();
            spans[0].ErrorMessage.Should().Be("Test exception");
            spans[0].Events.Should().HaveCount(1);
            spans[0].Events[0].Name.Should().Be("exception");
            spans[0].Events[0].Attributes.Should().ContainKey("exception.type");
            spans[0].Events[0].Attributes.Should().ContainKey("exception.message");
        }

        [Fact]
        public void Finish_ShouldSetEndTimeAndDuration()
        {
            // Arrange
            var registry = new TraceRegistry();
            var client = new TracingClient(registry);
            var span = client.StartSpan("Test", SpanKind.Internal);

            // Act
            System.Threading.Thread.Sleep(10);
            span.Finish();

            // Assert
            span.EndTime.Should().NotBeNull();
            span.DurationMs.Should().NotBeNull();
            span.DurationMs.Should().BeGreaterThan(0);
            var spans = registry.GetAllSpans();
            spans[0].IsActive.Should().BeFalse();
            registry.Count.Should().Be(1);
        }

        [Fact]
        public void Finish_WithEndTime_ShouldUseProvidedTime()
        {
            // Arrange
            var registry = new TraceRegistry();
            var client = new TracingClient(registry);
            var span = client.StartSpan("Test", SpanKind.Internal);
            var endTime = DateTimeOffset.UtcNow.AddMilliseconds(50);

            // Act
            span.Finish(endTime);

            // Assert
            span.EndTime.Should().Be(endTime);
            span.DurationMs.Should().Be(50);
        }

        [Fact]
        public void Dispose_ShouldFinishSpan()
        {
            // Arrange
            var registry = new TraceRegistry();
            var client = new TracingClient(registry);
            var span = client.StartSpan("Test", SpanKind.Internal);

            // Act
            span.Dispose();

            // Assert
            var spans = registry.GetAllSpans();
            spans[0].IsActive.Should().BeFalse();
            spans[0].EndTime.Should().NotBeNull();
            registry.Count.Should().Be(1);
        }

        [Fact]
        public void Dispose_WhenAlreadyFinished_ShouldNotDoubleFinish()
        {
            // Arrange
            var registry = new TraceRegistry();
            var client = new TracingClient(registry);
            var span = client.StartSpan("Test", SpanKind.Internal);
            span.Finish();
            var initialCount = registry.Count;

            // Act
            span.Dispose();

            // Assert
            registry.Count.Should().Be(initialCount);
        }
    }
}

