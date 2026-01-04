namespace JonjubNet.Observability.Logging.Core.Resilience
{
    /// <summary>
    /// Excepción lanzada cuando un circuit breaker está abierto y no permite ejecutar operaciones
    /// </summary>
    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message)
        {
        }

        public CircuitBreakerOpenException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

