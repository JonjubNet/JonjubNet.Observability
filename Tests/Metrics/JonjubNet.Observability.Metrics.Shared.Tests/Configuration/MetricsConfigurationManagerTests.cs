using FluentAssertions;
using JonjubNet.Observability.Metrics.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JonjubNet.Observability.Metrics.Shared.Tests.Configuration
{
    public class MetricsConfigurationManagerTests
    {
        [Fact]
        public void GetOptions_ShouldReturnOptions()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Metrics:Enabled"] = "true",
                    ["Metrics:ServiceName"] = "TestService"
                })
                .Build();
            var manager = new MetricsConfigurationManager(config);

            // Act
            var options = manager.GetOptions();

            // Assert
            options.Should().NotBeNull();
            options.Enabled.Should().BeTrue();
            options.ServiceName.Should().Be("TestService");
        }

        [Fact]
        public void Reload_ShouldReloadConfiguration()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Metrics:Enabled"] = "true"
                })
                .Build();
            var manager = new MetricsConfigurationManager(config);
            var firstOptions = manager.GetOptions();

            // Act
            manager.Reload();
            var secondOptions = manager.GetOptions();

            // Assert
            secondOptions.Should().NotBeNull();
        }
    }
}

