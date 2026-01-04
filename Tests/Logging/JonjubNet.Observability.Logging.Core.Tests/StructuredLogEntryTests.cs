using FluentAssertions;
using JonjubNet.Observability.Logging.Core;
using Xunit;
using CoreLogLevel = JonjubNet.Observability.Logging.Core.LogLevel;

namespace JonjubNet.Observability.Logging.Core.Tests
{
    public class StructuredLogEntryTests
    {
        [Fact]
        public void StructuredLogEntry_ShouldInitializeWithDefaults()
        {
            // Act
            var entry = new StructuredLogEntry();

            // Assert
            entry.Level.Should().Be(CoreLogLevel.Information);
            entry.Message.Should().BeEmpty();
            entry.Category.Should().BeEmpty();
            entry.Properties.Should().NotBeNull();
            entry.Tags.Should().NotBeNull();
            entry.Exception.Should().BeNull();
        }

        [Fact]
        public void StructuredLogEntry_WithAllProperties_ShouldSetAllProperties()
        {
            // Arrange
            var exception = new InvalidOperationException("Test");
            var properties = new Dictionary<string, object?> { ["prop1"] = "value1" };
            var tags = new Dictionary<string, string> { ["tag1"] = "value1" };

            // Act
            var entry = new StructuredLogEntry
            {
                Level = CoreLogLevel.Error,
                Message = "Test message",
                Category = "TestCategory",
                Exception = exception,
                Properties = properties,
                Tags = tags,
                Timestamp = DateTimeOffset.UtcNow
            };

            // Assert
            entry.Level.Should().Be(CoreLogLevel.Error);
            entry.Message.Should().Be("Test message");
            entry.Category.Should().Be("TestCategory");
            entry.Exception.Should().Be(exception);
            entry.Properties.Should().BeEquivalentTo(properties);
            entry.Tags.Should().BeEquivalentTo(tags);
        }

        [Fact]
        public void StructuredLogEntry_Timestamp_ShouldBeSet()
        {
            // Arrange
            var before = DateTimeOffset.UtcNow;

            // Act
            var entry = new StructuredLogEntry
            {
                Message = "Test",
                Timestamp = DateTimeOffset.UtcNow
            };

            var after = DateTimeOffset.UtcNow;

            // Assert
            entry.Timestamp.Should().BeOnOrAfter(before);
            entry.Timestamp.Should().BeOnOrBefore(after);
        }
    }
}

