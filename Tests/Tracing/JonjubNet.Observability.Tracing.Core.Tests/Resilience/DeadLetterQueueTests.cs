using FluentAssertions;
using JonjubNet.Observability.Tracing.Core;
using JonjubNet.Observability.Tracing.Core.Resilience;
using Xunit;

namespace JonjubNet.Observability.Tracing.Core.Tests.Resilience
{
    public class DeadLetterQueueTests
    {
        [Fact]
        public void Enqueue_ShouldAddFailedSpan()
        {
            // Arrange
            var dlq = new DeadLetterQueue();
            var span = new Span { SpanId = "span1", TraceId = "trace1", OperationName = "FailedOperation" };
            var failedSpan = new FailedSpan(span, "TestSink", 3, new Exception("Test error"));

            // Act
            var result = dlq.Enqueue(failedSpan);

            // Assert
            result.Should().BeTrue();
            dlq.Count.Should().Be(1);
        }

        [Fact]
        public void TryDequeue_ShouldReturnFailedSpan()
        {
            // Arrange
            var dlq = new DeadLetterQueue();
            var span = new Span { SpanId = "span1", TraceId = "trace1", OperationName = "FailedOperation" };
            var failedSpan = new FailedSpan(span, "TestSink", 3);
            dlq.Enqueue(failedSpan);

            // Act
            var result = dlq.TryDequeue(out var dequeued);

            // Assert
            result.Should().BeTrue();
            dequeued.Should().NotBeNull();
            dequeued!.Span.OperationName.Should().Be("FailedOperation");
            dequeued.SinkName.Should().Be("TestSink");
        }

        [Fact]
        public void TryDequeue_WhenEmpty_ShouldReturnFalse()
        {
            // Arrange
            var dlq = new DeadLetterQueue();

            // Act
            var result = dlq.TryDequeue(out var dequeued);

            // Assert
            result.Should().BeFalse();
            dequeued.Should().BeNull();
        }

        [Fact]
        public void GetAll_ShouldReturnAllFailedSpans()
        {
            // Arrange
            var dlq = new DeadLetterQueue();
            dlq.Enqueue(new FailedSpan(new Span { OperationName = "Op1" }, "Sink1", 1));
            dlq.Enqueue(new FailedSpan(new Span { OperationName = "Op2" }, "Sink2", 2));

            // Act
            var all = dlq.GetAll();

            // Assert
            all.Should().HaveCount(2);
            dlq.Count.Should().Be(0); // GetAll clears the queue
        }

        [Fact]
        public void Clear_ShouldRemoveAllSpans()
        {
            // Arrange
            var dlq = new DeadLetterQueue();
            dlq.Enqueue(new FailedSpan(new Span { OperationName = "Op1" }, "Sink1", 1));
            dlq.Enqueue(new FailedSpan(new Span { OperationName = "Op2" }, "Sink2", 2));

            // Act
            dlq.Clear();

            // Assert
            dlq.Count.Should().Be(0);
        }

        [Fact]
        public void GetStats_ShouldReturnStats()
        {
            // Arrange
            var dlq = new DeadLetterQueue(maxSize: 100);
            dlq.Enqueue(new FailedSpan(new Span { OperationName = "Op1" }, "Sink1", 1));

            // Act
            var stats = dlq.GetStats();

            // Assert
            stats.Should().NotBeNull();
            stats.Count.Should().Be(1);
            stats.MaxSize.Should().Be(100);
            stats.UtilizationPercent.Should().Be(1.0);
        }

        [Fact]
        public void Enqueue_WhenMaxSizeReached_ShouldRemoveOldest()
        {
            // Arrange
            var dlq = new DeadLetterQueue(maxSize: 2);
            dlq.Enqueue(new FailedSpan(new Span { OperationName = "Op1" }, "Sink1", 1));
            dlq.Enqueue(new FailedSpan(new Span { OperationName = "Op2" }, "Sink2", 2));

            // Act
            dlq.Enqueue(new FailedSpan(new Span { OperationName = "Op3" }, "Sink3", 3));

            // Assert
            dlq.Count.Should().Be(2);
            var all = dlq.GetAll();
            all.Should().NotContain(s => s.Span.OperationName == "Op1");
            all.Should().Contain(s => s.Span.OperationName == "Op3");
        }
    }
}

