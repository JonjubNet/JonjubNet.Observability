using System;
using System.Threading.Tasks;

namespace JonjubNet.Observability.Metrics.Core.Interfaces
{
    /// <summary>
    /// Interfaz para el gestor de circuit breakers por sink individual
    /// Permite tener un circuit breaker independiente para cada sink
    /// </summary>
    public interface ISinkCircuitBreakerManager
    {
        /// <summary>
        /// Ejecuta una operación con circuit breaker del sink
        /// </summary>
        Task<T> ExecuteWithCircuitBreakerAsync<T>(IMetricsSink sink, Func<Task<T>> operation);

        /// <summary>
        /// Ejecuta una acción con circuit breaker del sink
        /// </summary>
        Task ExecuteWithCircuitBreakerAsync(IMetricsSink sink, Func<Task> operation);
    }
}

