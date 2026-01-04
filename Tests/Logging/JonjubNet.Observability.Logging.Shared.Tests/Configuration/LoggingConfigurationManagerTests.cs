using FluentAssertions;
using JonjubNet.Observability.Logging.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JonjubNet.Observability.Logging.Shared.Tests.Configuration
{
    public class LoggingConfigurationManagerTests
    {
        [Fact]
        public void GetOptions_ShouldLoadFromConfiguration()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JonjubNet:Logging:Enabled"] = "true",
                    ["JonjubNet:Logging:ServiceName"] = "TestService"
                })
                .Build();
            var manager = new LoggingConfigurationManager(config);

            // Act
            var options = manager.GetOptions();

            // Assert
            options.Should().NotBeNull();
            options.Enabled.Should().BeTrue();
            options.ServiceName.Should().Be("TestService");
        }

        [Fact]
        public void GetOptions_WithNoConfiguration_ShouldReturnDefaults()
        {
            // Arrange
            var config = new ConfigurationBuilder().Build();
            var manager = new LoggingConfigurationManager(config);

            // Act
            var options = manager.GetOptions();

            // Assert
            options.Should().NotBeNull();
        }

        [Fact]
        public void Reload_ShouldForceReload()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JonjubNet:Logging:Enabled"] = "true"
                })
                .Build();
            var manager = new LoggingConfigurationManager(config);
            var firstOptions = manager.GetOptions();

            // Act
            manager.Reload();
            var secondOptions = manager.GetOptions();

            // Assert
            secondOptions.Should().NotBeNull();
        }
    }
}

