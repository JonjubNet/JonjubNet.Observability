using JonjubNet.Observability.Logging.Core.Interfaces;
using JonjubNet.Observability.Logging.Core.Filters;
using JonjubNet.Observability.Logging.Core.Enrichment;
using JonjubNet.Observability.Shared.Context;

namespace JonjubNet.Observability.Logging.Core
{
    /// <summary>
    /// Implementación del cliente de logging (Fast Path)
    /// Optimizado: Solo escribe al Registry - todos los sinks leen del Registry
    /// Similar a MetricsClient
    /// </summary>
    public class LoggingClient : ILoggingClient
    {
        private readonly LogRegistry _registry;
        private readonly LogScopeManager _scopeManager;
        private readonly LogFilter? _filter;
        private readonly LogEnricher? _enricher;

        public LoggingClient(
            LogRegistry registry,
            LogScopeManager? scopeManager = null,
            LogFilter? filter = null,
            LogEnricher? enricher = null)
        {
            _registry = registry;
            _scopeManager = scopeManager ?? new LogScopeManager();
            _filter = filter;
            _enricher = enricher;
        }

        public void LogTrace(string message, string? category = null, Dictionary<string, object?>? properties = null, Dictionary<string, string>? tags = null)
        {
            Log(LogLevel.Trace, message, null, category, properties, tags);
        }

        public void LogDebug(string message, string? category = null, Dictionary<string, object?>? properties = null, Dictionary<string, string>? tags = null)
        {
            Log(LogLevel.Debug, message, null, category, properties, tags);
        }

        public void LogInformation(string message, string? category = null, Dictionary<string, object?>? properties = null, Dictionary<string, string>? tags = null)
        {
            Log(LogLevel.Information, message, null, category, properties, tags);
        }

        public void LogWarning(string message, string? category = null, Dictionary<string, object?>? properties = null, Dictionary<string, string>? tags = null)
        {
            Log(LogLevel.Warning, message, null, category, properties, tags);
        }

        public void LogError(string message, Exception? exception = null, string? category = null, Dictionary<string, object?>? properties = null, Dictionary<string, string>? tags = null)
        {
            Log(LogLevel.Error, message, exception, category, properties, tags);
        }

        public void LogCritical(string message, Exception? exception = null, string? category = null, Dictionary<string, object?>? properties = null, Dictionary<string, string>? tags = null)
        {
            Log(LogLevel.Critical, message, exception, category, properties, tags);
        }

        // Cache de categorías internadas para reducir allocations de GC
        private static readonly Dictionary<string, string> _internedCategories = new();
        private static readonly object _categoryLock = new();
        private static readonly string DefaultCategory = string.Intern("General");

        private static string InternCategory(string? category)
        {
            if (string.IsNullOrEmpty(category))
                return DefaultCategory;

            lock (_categoryLock)
            {
                if (!_internedCategories.TryGetValue(category, out var interned))
                {
                    interned = string.Intern(category);
                    _internedCategories[category] = interned;
                }
                return interned;
            }
        }

        public void Log(LogLevel level, string message, Exception? exception = null, string? category = null, Dictionary<string, object?>? properties = null, Dictionary<string, string>? tags = null)
        {
            // SOLO escritura al Registry - todos los sinks leen del Registry
            var logEntry = new StructuredLogEntry
            {
                Level = level,
                Message = message,
                Category = InternCategory(category), // Usar string interning para categorías
                Timestamp = DateTimeOffset.UtcNow,
                Exception = exception,
                Properties = properties ?? new Dictionary<string, object?>(),
                Tags = tags ?? new Dictionary<string, string>()
            };

            // Aplicar propiedades del scope activo
            var activeScope = _scopeManager.GetCurrentScope();
            if (activeScope != null)
            {
                foreach (var prop in activeScope.Properties)
                {
                    if (!logEntry.Properties.ContainsKey(prop.Key))
                    {
                        logEntry.Properties[prop.Key] = prop.Value;
                    }
                }
            }

            // Aplicar enriquecimiento
            _enricher?.Enrich(logEntry);

            // Enriquecer automáticamente con CorrelationId del contexto compartido
            // Mejores prácticas: CorrelationId es el identificador único de la transacción
            // Optimizado: solo leer contexto si no está ya establecido
            var context = ObservabilityContext.Current;
            if (context != null)
            {
                // CorrelationId es el identificador principal (siempre debe estar disponible)
                if (!string.IsNullOrEmpty(context.CorrelationId) && string.IsNullOrEmpty(logEntry.CorrelationId))
                {
                    logEntry.CorrelationId = context.CorrelationId;
                }

                // SpanId para correlación de spans (tracing distribuido)
                if (!string.IsNullOrEmpty(context.SpanId))
                {
                    logEntry.Properties.TryAdd("SpanId", context.SpanId);
                }

                // TraceId para correlación de spans (tracing distribuido)
                if (!string.IsNullOrEmpty(context.TraceId))
                {
                    logEntry.Properties.TryAdd("TraceId", context.TraceId);
                }

                // SessionId y UserId opcionales (no se propagan entre microservicios)
                if (!string.IsNullOrEmpty(context.SessionId) && string.IsNullOrEmpty(logEntry.SessionId))
                {
                    logEntry.SessionId = context.SessionId;
                }
            }

            // Aplicar filtros
            if (_filter != null && !_filter.ShouldProcess(logEntry))
            {
                return; // No agregar al registry si el filtro lo rechaza
            }

            _registry.AddLog(logEntry);
        }

        public IDisposable BeginScope(string scopeName, Dictionary<string, object?>? properties = null)
        {
            return _scopeManager.BeginScope(scopeName, properties);
        }

        public IDisposable BeginOperation(string operationName, Dictionary<string, object?>? properties = null)
        {
            var startTime = DateTime.UtcNow;
            var scope = _scopeManager.BeginScope(operationName, properties);
            
            return new OperationScope(scope, operationName, startTime, this);
        }

        private class OperationScope : IDisposable
        {
            private readonly IDisposable _innerScope;
            private readonly string _operationName;
            private readonly DateTime _startTime;
            private readonly LoggingClient _client;
            private bool _disposed;

            public OperationScope(IDisposable innerScope, string operationName, DateTime startTime, LoggingClient client)
            {
                _innerScope = innerScope;
                _operationName = operationName;
                _startTime = startTime;
                _client = client;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                var duration = (long)(DateTime.UtcNow - _startTime).TotalMilliseconds;
                
                _client.LogInformation(
                    $"Operation '{_operationName}' completed",
                    category: "Operation",
                    properties: new Dictionary<string, object?> { { "DurationMs", duration } },
                    tags: new Dictionary<string, string> { { "operation", _operationName } }
                );

                _innerScope.Dispose();
                _disposed = true;
            }
        }
    }
}

