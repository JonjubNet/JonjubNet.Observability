using FluentAssertions;
using JonjubNet.Observability.Logging.Core;
using JonjubNet.Observability.Logging.Core.Resilience;
using Xunit;
using CoreLogLevel = JonjubNet.Observability.Logging.Core.LogLevel;

namespace JonjubNet.Observability.Logging.Core.Tests.Resilience
{
    public class DeadLetterQueueTests
    {
        [Fact]
        public void Enqueue_ShouldAddFailedLog()
        {
            // Arrange
            var dlq = new DeadLetterQueue();
            var logEntry = new StructuredLogEntry { Level = CoreLogLevel.Error, Message = "Failed log" };
            var failedLog = new FailedLog(logEntry, "TestSink", 3, new Exception("Test error"));

            // Act
            var result = dlq.Enqueue(failedLog);

            // Assert
            result.Should().BeTrue();
            dlq.Count.Should().Be(1);
        }

        [Fact]
        public void TryDequeue_ShouldReturnFailedLog()
        {
            // Arrange
            var dlq = new DeadLetterQueue();
            var logEntry = new StructuredLogEntry { Level = CoreLogLevel.Error, Message = "Failed log" };
            var failedLog = new FailedLog(logEntry, "TestSink", 3);
            dlq.Enqueue(failedLog);

            // Act
            var result = dlq.TryDequeue(out var dequeued);

            // Assert
            result.Should().BeTrue();
            dequeued.Should().NotBeNull();
            dequeued!.LogEntry.Message.Should().Be("Failed log");
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
        public void GetAll_ShouldReturnAllFailedLogs()
        {
            // Arrange
            var dlq = new DeadLetterQueue();
            dlq.Enqueue(new FailedLog(new StructuredLogEntry { Message = "Log1" }, "Sink1", 1));
            dlq.Enqueue(new FailedLog(new StructuredLogEntry { Message = "Log2" }, "Sink2", 2));

            // Act
            var all = dlq.GetAll();

            // Assert
            all.Should().HaveCount(2);
            dlq.Count.Should().Be(0); // GetAll clears the queue
        }

        [Fact]
        public void Clear_ShouldRemoveAllLogs()
        {
            // Arrange
            var dlq = new DeadLetterQueue();
            dlq.Enqueue(new FailedLog(new StructuredLogEntry { Message = "Log1" }, "Sink1", 1));
            dlq.Enqueue(new FailedLog(new StructuredLogEntry { Message = "Log2" }, "Sink2", 2));

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
            dlq.Enqueue(new FailedLog(new StructuredLogEntry { Message = "Log1" }, "Sink1", 1));

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
            dlq.Enqueue(new FailedLog(new StructuredLogEntry { Message = "Log1" }, "Sink1", 1));
            dlq.Enqueue(new FailedLog(new StructuredLogEntry { Message = "Log2" }, "Sink2", 2));

            // Act
            dlq.Enqueue(new FailedLog(new StructuredLogEntry { Message = "Log3" }, "Sink3", 3));

            // Assert
            dlq.Count.Should().Be(2);
            var all = dlq.GetAll();
            all.Should().NotContain(l => l.LogEntry.Message == "Log1");
            all.Should().Contain(l => l.LogEntry.Message == "Log3");
        }
    }
}

