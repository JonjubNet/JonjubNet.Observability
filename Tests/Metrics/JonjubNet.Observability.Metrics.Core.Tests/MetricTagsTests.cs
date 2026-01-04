using FluentAssertions;
using JonjubNet.Observability.Metrics.Core;
using Xunit;

namespace JonjubNet.Observability.Metrics.Core.Tests
{
    public class MetricTagsTests
    {
        [Fact]
        public void Create_WithNoTags_ShouldReturnEmptyDictionary()
        {
            // Act
            var tags = MetricTags.Create();

            // Assert
            tags.Should().NotBeNull();
            tags.Should().BeEmpty();
        }

        [Fact]
        public void Create_WithNullTags_ShouldReturnEmptyDictionary()
        {
            // Act - Create no acepta null directamente, pero acepta array vacío
            var tags = MetricTags.Create();

            // Assert
            tags.Should().NotBeNull();
            tags.Should().BeEmpty();
        }

        [Fact]
        public void Create_WithSingleTag_ShouldCreateDictionary()
        {
            // Act
            var tags = MetricTags.Create(("env", "production"));

            // Assert
            tags.Should().HaveCount(1);
            tags.Should().ContainKey("env").WhoseValue.Should().Be("production");
        }

        [Fact]
        public void Create_WithMultipleTags_ShouldCreateDictionary()
        {
            // Act
            var tags = MetricTags.Create(
                ("env", "production"),
                ("service", "api"),
                ("version", "1.0")
            );

            // Assert
            tags.Should().HaveCount(3);
            tags.Should().ContainKey("env").WhoseValue.Should().Be("production");
            tags.Should().ContainKey("service").WhoseValue.Should().Be("api");
            tags.Should().ContainKey("version").WhoseValue.Should().Be("1.0");
        }

        [Fact]
        public void Create_WithDuplicateKeys_ShouldUseLastValue()
        {
            // Act
            var tags = MetricTags.Create(
                ("key", "first"),
                ("key", "last")
            );

            // Assert
            tags.Should().HaveCount(1);
            tags.Should().ContainKey("key").WhoseValue.Should().Be("last");
        }

        [Fact]
        public void Combine_WithNoDictionaries_ShouldReturnEmptyDictionary()
        {
            // Act
            var result = MetricTags.Combine();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void Combine_WithSingleDictionary_ShouldReturnCopy()
        {
            // Arrange
            var dict1 = new Dictionary<string, string> { { "env", "test" } };

            // Act
            var result = MetricTags.Combine(dict1);

            // Assert
            result.Should().HaveCount(1);
            result.Should().ContainKey("env").WhoseValue.Should().Be("test");
            result.Should().NotBeSameAs(dict1); // Should be a copy
        }

        [Fact]
        public void Combine_WithMultipleDictionaries_ShouldMergeAll()
        {
            // Arrange
            var dict1 = new Dictionary<string, string> { { "env", "production" } };
            var dict2 = new Dictionary<string, string> { { "service", "api" } };
            var dict3 = new Dictionary<string, string> { { "version", "1.0" } };

            // Act
            var result = MetricTags.Combine(dict1, dict2, dict3);

            // Assert
            result.Should().HaveCount(3);
            result.Should().ContainKey("env").WhoseValue.Should().Be("production");
            result.Should().ContainKey("service").WhoseValue.Should().Be("api");
            result.Should().ContainKey("version").WhoseValue.Should().Be("1.0");
        }

        [Fact]
        public void Combine_WithOverlappingKeys_ShouldUseLastValue()
        {
            // Arrange
            var dict1 = new Dictionary<string, string> { { "key", "first" } };
            var dict2 = new Dictionary<string, string> { { "key", "last" } };

            // Act
            var result = MetricTags.Combine(dict1, dict2);

            // Assert
            result.Should().HaveCount(1);
            result.Should().ContainKey("key").WhoseValue.Should().Be("last");
        }

        [Fact]
        public void Combine_WithNullDictionary_ShouldSkipIt()
        {
            // Arrange
            var dict1 = new Dictionary<string, string> { { "env", "test" } };
            Dictionary<string, string>? dict2 = null;

            // Act
            var result = MetricTags.Combine(dict1, dict2!);

            // Assert
            result.Should().HaveCount(1);
            result.Should().ContainKey("env").WhoseValue.Should().Be("test");
        }

        [Fact]
        public void ReturnToPool_WithNull_ShouldNotThrow()
        {
            // Act
            var act = () => MetricTags.ReturnToPool(null!);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void ReturnToPool_WithDictionary_ShouldNotThrow()
        {
            // Arrange
            var tags = MetricTags.Create(("key", "value"));

            // Act
            var act = () => MetricTags.ReturnToPool(tags);

            // Assert
            act.Should().NotThrow();
        }
    }
}

