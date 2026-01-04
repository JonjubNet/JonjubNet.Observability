namespace JonjubNet.Observability.Tracing.Core
{
    /// <summary>
    /// Administrador de scopes de tracing (contexto temporal)
    /// Thread-safe usando AsyncLocal para mantener contexto por thread
    /// Similar a LogScopeManager
    /// </summary>
    public class TraceScopeManager
    {
        private static readonly AsyncLocal<TraceScope?> _currentScope = new();

        /// <summary>
        /// Obtiene el scope actual
        /// </summary>
        public TraceScope? GetCurrentScope()
        {
            return _currentScope.Value;
        }

        /// <summary>
        /// Inicia un nuevo scope
        /// </summary>
        public IDisposable BeginScope(string scopeName, Dictionary<string, object?>? properties = null)
        {
            var parentScope = _currentScope.Value;
            var newScope = new TraceScope(scopeName, properties, parentScope);
            _currentScope.Value = newScope;
            return new ScopeDisposable(newScope);
        }

        private class ScopeDisposable : IDisposable
        {
            private readonly TraceScope _scope;
            private volatile bool _disposed; // Thread-safe: volatile para lectura/escritura atómica

            public ScopeDisposable(TraceScope scope)
            {
                _scope = scope;
            }

            public void Dispose()
            {
                // Thread-safe: usar Interlocked para evitar double-dispose
                if (System.Threading.Interlocked.CompareExchange(ref _disposed, true, false) != false)
                    return; // Ya fue disposed por otro thread

                // Restaurar el scope padre
                _currentScope.Value = _scope.Parent;
            }
        }
    }

    /// <summary>
    /// Representa un scope de tracing (contexto temporal)
    /// </summary>
    public class TraceScope
    {
        public string Name { get; }
        public Dictionary<string, object?> Properties { get; }
        public TraceScope? Parent { get; }

        public TraceScope(string name, Dictionary<string, object?>? properties = null, TraceScope? parent = null)
        {
            Name = name;
            Properties = properties ?? new Dictionary<string, object?>();
            Parent = parent;
        }

        /// <summary>
        /// Obtiene todas las propiedades del scope y sus padres (heredadas)
        /// Optimizado: pre-estimar capacidad y usar TryAdd para evitar ContainsKey
        /// </summary>
        public Dictionary<string, object?> GetAllProperties()
        {
            // Contar propiedades totales para pre-estimar capacidad
            int totalProperties = 0;
            var current = this;
            while (current != null)
            {
                totalProperties += current.Properties.Count;
                current = current.Parent;
            }

            // Pre-allocate capacity
            var allProperties = new Dictionary<string, object?>(totalProperties);
            current = this;

            while (current != null)
            {
                foreach (var prop in current.Properties)
                {
                    // Optimizado: usar TryAdd en lugar de ContainsKey + indexer
                    allProperties.TryAdd(prop.Key, prop.Value);
                }
                current = current.Parent;
            }

            return allProperties;
        }
    }
}
