using FluentAssertions;
using JonjubNet.Observability.Tracing.Core;
using Xunit;

namespace JonjubNet.Observability.Tracing.Core.Tests
{
    public class TraceRegistryTests
    {
        [Fact]
        public void AddSpan_ShouldAddSpanToRegistry()
        {
            // Arrange
            var registry = new TraceRegistry();
            var span = new Span
            {
                SpanId = "span1",
                TraceId = "trace1",
                OperationName = "TestOperation"
            };

            // Act
            registry.AddSpan(span);

            // Assert
            registry.Count.Should().Be(1);
        }

        [Fact]
        public void AddSpan_WithNull_ShouldNotAdd()
        {
            // Arrange
            var registry = new TraceRegistry();

            // Act
            registry.AddSpan(null!);

            // Assert
            registry.Count.Should().Be(0);
        }

        [Fact]
        public void GetAllSpansAndClear_ShouldReturnAllSpansAndClear()
        {
            // Arrange
            var registry = new TraceRegistry();
            registry.AddSpan(new Span { SpanId = "span1", TraceId = "trace1", OperationName = "Op1" });
            registry.AddSpan(new Span { SpanId = "span2", TraceId = "trace1", OperationName = "Op2" });

            // Act
            var spans = registry.GetAllSpansAndClear();

            // Assert
            spans.Should().HaveCount(2);
            registry.Count.Should().Be(0);
        }

        [Fact]
        public void GetAllSpans_ShouldReturnAllSpansWithoutClearing()
        {
            // Arrange
            var registry = new TraceRegistry();
            registry.AddSpan(new Span { SpanId = "span1", TraceId = "trace1", OperationName = "Op1" });
            registry.AddSpan(new Span { SpanId = "span2", TraceId = "trace1", OperationName = "Op2" });

            // Act
            var spans = registry.GetAllSpans();

            // Assert
            spans.Should().HaveCount(2);
            registry.Count.Should().Be(2);
        }

        [Fact]
        public void Clear_ShouldRemoveAllSpans()
        {
            // Arrange
            var registry = new TraceRegistry();
            registry.AddSpan(new Span { SpanId = "span1", TraceId = "trace1", OperationName = "Op1" });
            registry.AddSpan(new Span { SpanId = "span2", TraceId = "trace1", OperationName = "Op2" });

            // Act
            registry.Clear();

            // Assert
            registry.Count.Should().Be(0);
        }

        [Fact]
        public void GetSpansByTraceId_ShouldReturnFilteredSpans()
        {
            // Arrange
            var registry = new TraceRegistry();
            registry.AddSpan(new Span { SpanId = "span1", TraceId = "trace1", OperationName = "Op1" });
            registry.AddSpan(new Span { SpanId = "span2", TraceId = "trace1", OperationName = "Op2" });
            registry.AddSpan(new Span { SpanId = "span3", TraceId = "trace2", OperationName = "Op3" });

            // Act
            var trace1Spans = registry.GetSpansByTraceId("trace1");
            var trace2Spans = registry.GetSpansByTraceId("trace2");

            // Assert
            trace1Spans.Should().HaveCount(2);
            trace2Spans.Should().HaveCount(1);
            trace1Spans.All(s => s.TraceId == "trace1").Should().BeTrue();
            trace2Spans.All(s => s.TraceId == "trace2").Should().BeTrue();
        }

        [Fact]
        public void GetSpansByOperation_ShouldReturnFilteredSpans()
        {
            // Arrange
            var registry = new TraceRegistry();
            registry.AddSpan(new Span { SpanId = "span1", TraceId = "trace1", OperationName = "Operation1" });
            registry.AddSpan(new Span { SpanId = "span2", TraceId = "trace1", OperationName = "Operation2" });
            registry.AddSpan(new Span { SpanId = "span3", TraceId = "trace2", OperationName = "Operation1" });

            // Act
            var op1Spans = registry.GetSpansByOperation("Operation1");
            var op2Spans = registry.GetSpansByOperation("Operation2");

            // Assert
            op1Spans.Should().HaveCount(2);
            op2Spans.Should().HaveCount(1);
            op1Spans.All(s => s.OperationName == "Operation1").Should().BeTrue();
            op2Spans.All(s => s.OperationName == "Operation2").Should().BeTrue();
        }

        [Fact]
        public void GetSpansByStatus_ShouldReturnFilteredSpans()
        {
            // Arrange
            var registry = new TraceRegistry();
            registry.AddSpan(new Span { SpanId = "span1", TraceId = "trace1", OperationName = "Op1", Status = SpanStatus.Ok });
            registry.AddSpan(new Span { SpanId = "span2", TraceId = "trace1", OperationName = "Op2", Status = SpanStatus.Error });
            registry.AddSpan(new Span { SpanId = "span3", TraceId = "trace2", OperationName = "Op3", Status = SpanStatus.Ok });

            // Act
            var okSpans = registry.GetSpansByStatus(SpanStatus.Ok);
            var errorSpans = registry.GetSpansByStatus(SpanStatus.Error);

            // Assert
            okSpans.Should().HaveCount(2);
            errorSpans.Should().HaveCount(1);
            okSpans.All(s => s.Status == SpanStatus.Ok).Should().BeTrue();
            errorSpans.All(s => s.Status == SpanStatus.Error).Should().BeTrue();
        }

        [Fact]
        public void MaxSize_ShouldLimitRegistrySize()
        {
            // Arrange
            var registry = new TraceRegistry();
            registry.MaxSize = 3;

            // Act
            registry.AddSpan(new Span { SpanId = "span1", TraceId = "trace1", OperationName = "Op1" });
            registry.AddSpan(new Span { SpanId = "span2", TraceId = "trace1", OperationName = "Op2" });
            registry.AddSpan(new Span { SpanId = "span3", TraceId = "trace1", OperationName = "Op3" });
            registry.AddSpan(new Span { SpanId = "span4", TraceId = "trace1", OperationName = "Op4" }); // Debe eliminar span1

            // Assert
            registry.Count.Should().Be(3);
        }

        [Fact]
        public void MaxSize_SetSmaller_ShouldRemoveOldSpans()
        {
            // Arrange
            var registry = new TraceRegistry();
            registry.AddSpan(new Span { SpanId = "span1", TraceId = "trace1", OperationName = "Op1" });
            registry.AddSpan(new Span { SpanId = "span2", TraceId = "trace1", OperationName = "Op2" });
            registry.AddSpan(new Span { SpanId = "span3", TraceId = "trace1", OperationName = "Op3" });
            registry.AddSpan(new Span { SpanId = "span4", TraceId = "trace1", OperationName = "Op4" });

            // Act
            registry.MaxSize = 2;

            // Assert
            registry.Count.Should().Be(2);
        }

        [Fact]
        public void Count_ShouldReturnCorrectCount()
        {
            // Arrange
            var registry = new TraceRegistry();

            // Act
            registry.AddSpan(new Span { SpanId = "span1", TraceId = "trace1", OperationName = "Op1" });
            registry.AddSpan(new Span { SpanId = "span2", TraceId = "trace1", OperationName = "Op2" });

            // Assert
            registry.Count.Should().Be(2);
        }
    }
}

