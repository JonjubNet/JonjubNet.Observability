using System.Collections.Concurrent;

namespace JonjubNet.Observability.Logging.Core
{
    /// <summary>
    /// Administrador de scopes de logging (contexto temporal)
    /// Thread-safe usando AsyncLocal para mantener contexto por thread
    /// </summary>
    public class LogScopeManager
    {
        private static readonly AsyncLocal<LogScope?> _currentScope = new();

        /// <summary>
        /// Obtiene el scope actual
        /// </summary>
        public LogScope? GetCurrentScope()
        {
            return _currentScope.Value;
        }

        /// <summary>
        /// Inicia un nuevo scope
        /// </summary>
        public IDisposable BeginScope(string scopeName, Dictionary<string, object?>? properties = null)
        {
            var parentScope = _currentScope.Value;
            var newScope = new LogScope(scopeName, properties, parentScope);
            _currentScope.Value = newScope;
            return new ScopeDisposable(newScope);
        }

        private class ScopeDisposable : IDisposable
        {
            private readonly LogScope _scope;
            private bool _disposed;

            public ScopeDisposable(LogScope scope)
            {
                _scope = scope;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                // Restaurar el scope padre
                _currentScope.Value = _scope.Parent;
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Representa un scope de logging (contexto temporal)
    /// </summary>
    public class LogScope
    {
        public string Name { get; }
        public Dictionary<string, object?> Properties { get; }
        public LogScope? Parent { get; }

        public LogScope(string name, Dictionary<string, object?>? properties = null, LogScope? parent = null)
        {
            Name = name;
            Properties = properties ?? new Dictionary<string, object?>();
            Parent = parent;
        }

        /// <summary>
        /// Obtiene todas las propiedades del scope y sus padres (heredadas)
        /// </summary>
        public Dictionary<string, object?> GetAllProperties()
        {
            var allProperties = new Dictionary<string, object?>();
            var current = this;

            while (current != null)
            {
                foreach (var prop in current.Properties)
                {
                    if (!allProperties.ContainsKey(prop.Key))
                    {
                        allProperties[prop.Key] = prop.Value;
                    }
                }
                current = current.Parent;
            }

            return allProperties;
        }
    }
}

