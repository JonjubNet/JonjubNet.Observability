using FluentAssertions;
using JonjubNet.Observability.Logging.Core;
using JonjubNet.Observability.Logging.Core.Filters;
using Xunit;
using CoreLogLevel = JonjubNet.Observability.Logging.Core.LogLevel;

namespace JonjubNet.Observability.Logging.Core.Tests.Filters
{
    public class LogFilterTests
    {
        [Fact]
        public void ShouldProcess_WithMinLevel_ShouldFilterBelowMinLevel()
        {
            // Arrange
            var options = new FilterOptions { MinLevel = CoreLogLevel.Warning };
            var filter = new LogFilter(options);
            var infoLog = new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Info" };
            var warningLog = new StructuredLogEntry { Level = CoreLogLevel.Warning, Message = "Warning" };

            // Act & Assert
            filter.ShouldProcess(infoLog).Should().BeFalse();
            filter.ShouldProcess(warningLog).Should().BeTrue();
        }

        [Fact]
        public void ShouldProcess_WithMaxLevel_ShouldFilterAboveMaxLevel()
        {
            // Arrange
            var options = new FilterOptions { MaxLevel = CoreLogLevel.Warning };
            var filter = new LogFilter(options);
            var warningLog = new StructuredLogEntry { Level = CoreLogLevel.Warning, Message = "Warning" };
            var errorLog = new StructuredLogEntry { Level = CoreLogLevel.Error, Message = "Error" };

            // Act & Assert
            filter.ShouldProcess(warningLog).Should().BeTrue();
            filter.ShouldProcess(errorLog).Should().BeFalse();
        }

        [Fact]
        public void ShouldProcess_WithExcludedCategories_ShouldFilterExcluded()
        {
            // Arrange
            var options = new FilterOptions 
            { 
                ExcludedCategories = new List<string> { "ExcludedCategory" } 
            };
            var filter = new LogFilter(options);
            var excludedLog = new StructuredLogEntry { Level = CoreLogLevel.Information, Category = "ExcludedCategory", Message = "Test" };
            var allowedLog = new StructuredLogEntry { Level = CoreLogLevel.Information, Category = "AllowedCategory", Message = "Test" };

            // Act & Assert
            filter.ShouldProcess(excludedLog).Should().BeFalse();
            filter.ShouldProcess(allowedLog).Should().BeTrue();
        }

        [Fact]
        public void ShouldProcess_WithAllowedCategories_ShouldOnlyAllowSpecified()
        {
            // Arrange
            var options = new FilterOptions 
            { 
                AllowedCategories = new List<string> { "AllowedCategory" } 
            };
            var filter = new LogFilter(options);
            var allowedLog = new StructuredLogEntry { Level = CoreLogLevel.Information, Category = "AllowedCategory", Message = "Test" };
            var excludedLog = new StructuredLogEntry { Level = CoreLogLevel.Information, Category = "OtherCategory", Message = "Test" };

            // Act & Assert
            filter.ShouldProcess(allowedLog).Should().BeTrue();
            filter.ShouldProcess(excludedLog).Should().BeFalse();
        }

        [Fact]
        public void ShouldProcess_WithExcludedTags_ShouldFilterByTags()
        {
            // Arrange
            var options = new FilterOptions 
            { 
                ExcludedTags = new Dictionary<string, string> { ["env"] = "test" } 
            };
            var filter = new LogFilter(options);
            var excludedLog = new StructuredLogEntry 
            { 
                Level = CoreLogLevel.Information, 
                Message = "Test",
                Tags = new Dictionary<string, string> { ["env"] = "test" }
            };
            var allowedLog = new StructuredLogEntry 
            { 
                Level = CoreLogLevel.Information, 
                Message = "Test",
                Tags = new Dictionary<string, string> { ["env"] = "prod" }
            };

            // Act & Assert
            filter.ShouldProcess(excludedLog).Should().BeFalse();
            filter.ShouldProcess(allowedLog).Should().BeTrue();
        }

        [Fact]
        public void ShouldProcess_WithExcludedMessagePatterns_ShouldFilterByRegex()
        {
            // Arrange
            var options = new FilterOptions 
            { 
                ExcludedMessagePatterns = new List<string> { @"^Sensitive.*" } 
            };
            var filter = new LogFilter(options);
            var excludedLog = new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Sensitive data here" };
            var allowedLog = new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Normal message" };

            // Act & Assert
            filter.ShouldProcess(excludedLog).Should().BeFalse();
            filter.ShouldProcess(allowedLog).Should().BeTrue();
        }

        [Fact]
        public void ShouldProcess_WithNoFilters_ShouldAllowAll()
        {
            // Arrange
            var filter = new LogFilter();
            var log = new StructuredLogEntry { Level = CoreLogLevel.Information, Message = "Test" };

            // Act & Assert
            filter.ShouldProcess(log).Should().BeTrue();
        }
    }
}

