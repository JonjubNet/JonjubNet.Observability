using FluentAssertions;
using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.Core.Utils;
using Xunit;

namespace JonjubNet.Observability.Metrics.Shared.Tests.Utils
{
    public class CollectionPoolTests
    {
        [Fact]
        public void RentDictionary_ShouldReturnDictionary()
        {
            // Act
            var dict = CollectionPool.RentDictionary();

            // Assert
            dict.Should().NotBeNull();
            dict.Should().BeEmpty();
        }

        [Fact]
        public void ReturnDictionary_ShouldClearAndReturnToPool()
        {
            // Arrange
            var dict = CollectionPool.RentDictionary();
            dict["key"] = "value";

            // Act
            CollectionPool.ReturnDictionary(dict);
            var dict2 = CollectionPool.RentDictionary();

            // Assert
            dict2.Should().BeEmpty();
        }
    }
}
