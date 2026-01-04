namespace JonjubNet.Observability.Tracing.Core.Interfaces
{
    /// <summary>
    /// Interfaz del cliente de tracing
    /// Similar a ILoggingClient e IMetricsClient pero para traces/spans
    /// </summary>
    public interface ITracingClient
    {
        /// <summary>
        /// Inicia un nuevo span (trazabilidad)
        /// </summary>
        ISpan StartSpan(string operationName, SpanKind kind = SpanKind.Internal, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Crea un span hijo del span actual
        /// </summary>
        ISpan StartChildSpan(string operationName, SpanKind kind = SpanKind.Internal, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Obtiene el span actual activo
        /// </summary>
        ISpan? GetCurrentSpan();

        /// <summary>
        /// Crea un scope de tracing (contexto temporal)
        /// </summary>
        IDisposable BeginScope(string scopeName, Dictionary<string, object?>? properties = null);

        /// <summary>
        /// Inicia una operación con tracing automático (para medir duración)
        /// </summary>
        IDisposable BeginOperation(string operationName, Dictionary<string, string>? tags = null);
    }
}
