namespace JonjubNet.Observability.Logging.Core.Interfaces
{
    /// <summary>
    /// Interfaz para el gestor de circuit breakers por sink individual
    /// Permite tener un circuit breaker independiente para cada sink
    /// Similar a ISinkCircuitBreakerManager de Metrics
    /// </summary>
    public interface ISinkCircuitBreakerManager
    {
        /// <summary>
        /// Ejecuta una operación con circuit breaker del sink
        /// </summary>
        Task<T> ExecuteWithCircuitBreakerAsync<T>(ILogSink sink, Func<Task<T>> operation);

        /// <summary>
        /// Ejecuta una acción con circuit breaker del sink
        /// </summary>
        Task ExecuteWithCircuitBreakerAsync(ILogSink sink, Func<Task> operation);
    }
}

