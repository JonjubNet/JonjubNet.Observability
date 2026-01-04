using FluentAssertions;
using JonjubNet.Observability.Shared.Utils;
using System.Text.Json;
using Xunit;

namespace JonjubNet.Observability.Metrics.Shared.Tests.Utils
{
    public class JsonSerializerOptionsCacheTests
    {
        [Fact]
        public void GetDefault_ShouldReturnCachedOptions()
        {
            // Act
            var options1 = JsonSerializerOptionsCache.GetDefault();
            var options2 = JsonSerializerOptionsCache.GetDefault();

            // Assert
            options1.Should().BeSameAs(options2);
            options1.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
        }

        [Fact]
        public void GetOrCreate_WithSameKey_ShouldReturnSameInstance()
        {
            // Act
            var options1 = JsonSerializerOptionsCache.GetOrCreate("test", () => new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var options2 = JsonSerializerOptionsCache.GetOrCreate("test", () => new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            // Assert
            options1.Should().BeSameAs(options2);
        }

        [Fact]
        public void GetOrCreate_WithDifferentKeys_ShouldReturnDifferentInstances()
        {
            // Act
            var options1 = JsonSerializerOptionsCache.GetOrCreate("key1", () => new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var options2 = JsonSerializerOptionsCache.GetOrCreate("key2", () => new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            // Assert
            options1.Should().NotBeSameAs(options2);
        }

        [Fact]
        public void GetOrCreate_WithIndented_ShouldHaveWriteIndentedTrue()
        {
            // Act
            var options = JsonSerializerOptionsCache.GetOrCreate("indented", () => new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true 
            });

            // Assert
            options.WriteIndented.Should().BeTrue();
        }
    }
}
