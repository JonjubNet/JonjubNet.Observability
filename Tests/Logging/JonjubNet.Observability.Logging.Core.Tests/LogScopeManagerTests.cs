using FluentAssertions;
using JonjubNet.Observability.Logging.Core;
using Xunit;

namespace JonjubNet.Observability.Logging.Core.Tests
{
    public class LogScopeManagerTests
    {
        [Fact]
        public void BeginScope_ShouldCreateScope()
        {
            // Arrange
            var manager = new LogScopeManager();

            // Act
            using (manager.BeginScope("TestScope"))
            {
                var scope = manager.GetCurrentScope();
                
                // Assert
                scope.Should().NotBeNull();
                scope!.Name.Should().Be("TestScope");
            }
        }

        [Fact]
        public void BeginScope_WithProperties_ShouldIncludeProperties()
        {
            // Arrange
            var manager = new LogScopeManager();
            var properties = new Dictionary<string, object?> { ["key1"] = "value1" };

            // Act
            using (manager.BeginScope("TestScope", properties))
            {
                var scope = manager.GetCurrentScope();
                
                // Assert
                scope.Should().NotBeNull();
                scope!.Properties.Should().ContainKey("key1");
                scope.Properties["key1"].Should().Be("value1");
            }
        }

        [Fact]
        public void BeginScope_Nested_ShouldCreateNestedScopes()
        {
            // Arrange
            var manager = new LogScopeManager();

            // Act
            using (manager.BeginScope("OuterScope"))
            {
                var outerScope = manager.GetCurrentScope();
                
                using (manager.BeginScope("InnerScope"))
                {
                    var innerScope = manager.GetCurrentScope();
                    
                    // Assert
                    innerScope.Should().NotBeNull();
                    innerScope!.Name.Should().Be("InnerScope");
                    innerScope.Parent.Should().Be(outerScope);
                }
                
                // After inner scope disposal, should return to outer
                var currentAfterInner = manager.GetCurrentScope();
                currentAfterInner.Should().Be(outerScope);
            }
        }

        [Fact]
        public void GetCurrentScope_WhenNoScope_ShouldReturnNull()
        {
            // Arrange
            var manager = new LogScopeManager();

            // Act
            var scope = manager.GetCurrentScope();

            // Assert
            scope.Should().BeNull();
        }

        [Fact]
        public void BeginScope_Dispose_ShouldRestoreParentScope()
        {
            // Arrange
            var manager = new LogScopeManager();

            // Act
            LogScope? outerScope;
            using (manager.BeginScope("OuterScope"))
            {
                outerScope = manager.GetCurrentScope();
                
                using (manager.BeginScope("InnerScope"))
                {
                    // Inner scope active
                }
                
                // After inner disposal
                var current = manager.GetCurrentScope();
                current.Should().Be(outerScope);
            }
            
            // After outer disposal
            var final = manager.GetCurrentScope();
            final.Should().BeNull();
        }

        [Fact]
        public void LogScope_GetAllProperties_ShouldIncludeParentProperties()
        {
            // Arrange
            var manager = new LogScopeManager();

            // Act
            using (manager.BeginScope("OuterScope", new Dictionary<string, object?> { ["outer"] = "value" }))
            {
                using (manager.BeginScope("InnerScope", new Dictionary<string, object?> { ["inner"] = "value" }))
                {
                    var innerScope = manager.GetCurrentScope();
                    var allProperties = innerScope!.GetAllProperties();
                    
                    // Assert
                    allProperties.Should().ContainKey("outer");
                    allProperties.Should().ContainKey("inner");
                }
            }
        }
    }
}

