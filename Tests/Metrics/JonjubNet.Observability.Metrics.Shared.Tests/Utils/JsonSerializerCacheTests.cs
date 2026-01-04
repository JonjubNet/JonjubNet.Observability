using FluentAssertions;
using JonjubNet.Observability.Metrics.Shared.Utils;
using System.Text.Json;
using Xunit;

namespace JonjubNet.Observability.Metrics.Shared.Tests.Utils
{
    public class JsonSerializerCacheTests
    {
        [Fact]
        public void GetDefaultOptions_ShouldReturnSameInstance()
        {
            // Act
            var options1 = JsonSerializerCache.GetDefaultOptions();
            var options2 = JsonSerializerCache.GetDefaultOptions();

            // Assert
            options1.Should().BeSameAs(options2); // Debe ser la misma instancia (cached)
        }

        [Fact]
        public void GetIndentedOptions_ShouldReturnSameInstance()
        {
            // Act
            var options1 = JsonSerializerCache.GetIndentedOptions();
            var options2 = JsonSerializerCache.GetIndentedOptions();

            // Assert
            options1.Should().BeSameAs(options2); // Debe ser la misma instancia (cached)
        }

        [Fact]
        public void GetDefaultOptions_ShouldHaveCamelCaseNaming()
        {
            // Act
            var options = JsonSerializerCache.GetDefaultOptions();

            // Assert
            options.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
        }

        [Fact]
        public void CreateCustomOptions_ShouldCreateNewInstance()
        {
            // Act
            var options1 = JsonSerializerCache.CreateCustomOptions();
            var options2 = JsonSerializerCache.CreateCustomOptions();

            // Assert
            options1.Should().NotBeSameAs(options2); // Debe ser instancias diferentes
        }
    }
}

