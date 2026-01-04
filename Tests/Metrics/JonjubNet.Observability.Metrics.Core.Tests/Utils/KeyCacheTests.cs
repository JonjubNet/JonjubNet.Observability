using FluentAssertions;
using JonjubNet.Observability.Metrics.Core.Utils;
using Xunit;

namespace JonjubNet.Observability.Metrics.Core.Tests.Utils
{
    public class KeyCacheTests
    {
        [Fact]
        public void CreateKey_WithNullTags_ShouldReturnEmptyString()
        {
            // Act
            var key = KeyCache.CreateKey(null);

            // Assert
            key.Should().BeEmpty();
        }

        [Fact]
        public void CreateKey_WithEmptyTags_ShouldReturnEmptyString()
        {
            // Arrange
            var tags = new Dictionary<string, string>();

            // Act
            var key = KeyCache.CreateKey(tags);

            // Assert
            key.Should().BeEmpty();
        }

        [Fact]
        public void CreateKey_WithSingleTag_ShouldCreateKey()
        {
            // Arrange
            var tags = new Dictionary<string, string> { { "env", "production" } };

            // Act
            var key = KeyCache.CreateKey(tags);

            // Assert
            key.Should().Be("env=production");
        }

        [Fact]
        public void CreateKey_WithMultipleTags_ShouldCreateSortedKey()
        {
            // Arrange
            var tags = new Dictionary<string, string>
            {
                { "service", "api" },
                { "env", "production" },
                { "version", "1.0" }
            };

            // Act
            var key = KeyCache.CreateKey(tags);

            // Assert
            key.Should().Be("env=production,service=api,version=1.0"); // Sorted by key
        }

        [Fact]
        public void CreateKey_WithSameTags_ShouldReturnCachedKey()
        {
            // Arrange
            var tags = new Dictionary<string, string> { { "env", "test" } };

            // Act
            var key1 = KeyCache.CreateKey(tags);
            var key2 = KeyCache.CreateKey(tags);

            // Assert
            key1.Should().Be(key2);
            key1.Should().Be("env=test");
        }

        [Fact]
        public void CreateKey_WithDifferentTags_ShouldReturnDifferentKeys()
        {
            // Arrange
            var tags1 = new Dictionary<string, string> { { "env", "test" } };
            var tags2 = new Dictionary<string, string> { { "env", "prod" } };

            // Act
            var key1 = KeyCache.CreateKey(tags1);
            var key2 = KeyCache.CreateKey(tags2);

            // Assert
            key1.Should().NotBe(key2);
            key1.Should().Be("env=test");
            key2.Should().Be("env=prod");
        }

        [Fact]
        public void Clear_ShouldClearCache()
        {
            // Arrange
            var tags = new Dictionary<string, string> { { "env", "test" } };
            KeyCache.CreateKey(tags); // Populate cache
            var initialCount = KeyCache.Count;

            // Act
            KeyCache.Clear();

            // Assert
            KeyCache.Count.Should().Be(0);
            initialCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Count_ShouldReturnCacheSize()
        {
            // Arrange
            KeyCache.Clear();
            var tags1 = new Dictionary<string, string> { { "env", "test" } };
            var tags2 = new Dictionary<string, string> { { "service", "api" } };

            // Act
            KeyCache.CreateKey(tags1);
            KeyCache.CreateKey(tags2);

            // Assert
            KeyCache.Count.Should().BeGreaterThanOrEqualTo(2);
        }
    }
}

