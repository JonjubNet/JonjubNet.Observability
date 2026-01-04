namespace JonjubNet.Observability.Logging.Core.Interfaces
{
    /// <summary>
    /// Interfaz del cliente de logging
    /// Similar a IMetricsClient pero para logs
    /// </summary>
    public interface ILoggingClient
    {
        /// <summary>
        /// Registra un log con nivel Trace
        /// </summary>
        void LogTrace(string message, string? category = null, Dictionary<string, object?>? properties = null, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Registra un log con nivel Debug
        /// </summary>
        void LogDebug(string message, string? category = null, Dictionary<string, object?>? properties = null, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Registra un log con nivel Information
        /// </summary>
        void LogInformation(string message, string? category = null, Dictionary<string, object?>? properties = null, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Registra un log con nivel Warning
        /// </summary>
        void LogWarning(string message, string? category = null, Dictionary<string, object?>? properties = null, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Registra un log con nivel Error
        /// </summary>
        void LogError(string message, Exception? exception = null, string? category = null, Dictionary<string, object?>? properties = null, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Registra un log con nivel Critical
        /// </summary>
        void LogCritical(string message, Exception? exception = null, string? category = null, Dictionary<string, object?>? properties = null, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Registra un log con nivel personalizado
        /// </summary>
        void Log(LogLevel level, string message, Exception? exception = null, string? category = null, Dictionary<string, object?>? properties = null, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Crea un scope de logging (contexto temporal)
        /// </summary>
        IDisposable BeginScope(string scopeName, Dictionary<string, object?>? properties = null);

        /// <summary>
        /// Inicia una operación (para medir duración)
        /// </summary>
        IDisposable BeginOperation(string operationName, Dictionary<string, object?>? properties = null);
    }
}

