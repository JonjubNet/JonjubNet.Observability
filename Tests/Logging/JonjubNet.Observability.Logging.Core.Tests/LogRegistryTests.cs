using FluentAssertions;
using JonjubNet.Observability.Logging.Core;
using Xunit;
using CoreLogLevel = JonjubNet.Observability.Logging.Core.LogLevel;

namespace JonjubNet.Observability.Logging.Core.Tests
{
    public class LogRegistryTests
    {
        [Fact]
        public void AddLog_ShouldAddLogToRegistry()
        {
            // Arrange
            var registry = new LogRegistry();
            var logEntry = new StructuredLogEntry
            {
                Level = CoreLogLevel.Information,
                Message = "Test message",
                Category = "TestCategory"
            };

            // Act
            registry.AddLog(logEntry);

            // Assert
            registry.Count.Should().Be(1);
        }

        [Fact]
        public void AddLog_WithNull_ShouldNotAdd()
        {
            // Arrange
            var registry = new LogRegistry();

            // Act
            registry.AddLog(null!);

            // Assert
            registry.Count.Should().Be(0);
        }

        [Fact]
        public void GetAllLogsAndClear_ShouldReturnAllLogsAndClear()
        {
            // Arrange
            var registry = new LogRegistry();
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Message 1" });
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Warning, Message = "Message 2" });

            // Act
            var logs = registry.GetAllLogsAndClear();

            // Assert
            logs.Should().HaveCount(2);
            registry.Count.Should().Be(0);
        }

        [Fact]
        public void GetAllLogs_ShouldReturnAllLogsWithoutClearing()
        {
            // Arrange
            var registry = new LogRegistry();
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Message 1" });
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Warning, Message = "Message 2" });

            // Act
            var logs = registry.GetAllLogs();

            // Assert
            logs.Should().HaveCount(2);
            registry.Count.Should().Be(2);
        }

        [Fact]
        public void Clear_ShouldRemoveAllLogs()
        {
            // Arrange
            var registry = new LogRegistry();
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Message 1" });
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Warning, Message = "Message 2" });

            // Act
            registry.Clear();

            // Assert
            registry.Count.Should().Be(0);
        }

        [Fact]
        public void GetLogsByLevel_ShouldReturnFilteredLogs()
        {
            // Arrange
            var registry = new LogRegistry();
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Info 1" });
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Warning, Message = "Warning 1" });
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Info 2" });

            // Act
            var infoLogs = registry.GetLogsByLevel(CoreLogLevel.Information);
            var warningLogs = registry.GetLogsByLevel(CoreLogLevel.Warning);

            // Assert
            infoLogs.Should().HaveCount(2);
            warningLogs.Should().HaveCount(1);
            infoLogs.All(l => l.Level == CoreLogLevel.Information).Should().BeTrue();
            warningLogs.All(l => l.Level == CoreLogLevel.Warning).Should().BeTrue();
        }

        [Fact]
        public void GetLogsByCategory_ShouldReturnFilteredLogs()
        {
            // Arrange
            var registry = new LogRegistry();
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Message 1", Category = "Category1" });
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Message 2", Category = "Category2" });
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Message 3", Category = "Category1" });

            // Act
            var category1Logs = registry.GetLogsByCategory("Category1");
            var category2Logs = registry.GetLogsByCategory("Category2");

            // Assert
            category1Logs.Should().HaveCount(2);
            category2Logs.Should().HaveCount(1);
            category1Logs.All(l => l.Category == "Category1").Should().BeTrue();
            category2Logs.All(l => l.Category == "Category2").Should().BeTrue();
        }

        [Fact]
        public void MaxSize_ShouldLimitRegistrySize()
        {
            // Arrange
            var registry = new LogRegistry();
            registry.MaxSize = 3;

            // Act
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Message 1" });
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Message 2" });
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Message 3" });
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Message 4" }); // Debe eliminar Message 1

            // Assert
            registry.Count.Should().Be(3);
        }

        [Fact]
        public void MaxSize_SetSmaller_ShouldRemoveOldLogs()
        {
            // Arrange
            var registry = new LogRegistry();
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Message 1" });
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Message 2" });
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Message 3" });
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Message 4" });

            // Act
            registry.MaxSize = 2;

            // Assert
            registry.Count.Should().Be(2);
        }

        [Fact]
        public void Count_ShouldReturnCorrectCount()
        {
            // Arrange
            var registry = new LogRegistry();

            // Act
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Message 1" });
            registry.AddLog(new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Message 2" });

            // Assert
            registry.Count.Should().Be(2);
        }
    }
}

