using JonjubNet.Observability.Metrics.Core.Resilience;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Metrics.Shared.Resilience
{
    /// <summary>
    /// Circuit Breaker para métricas
    /// </summary>
    public class MetricCircuitBreaker
    {
        private readonly int _failureThreshold;
        private readonly TimeSpan _openDuration;
        private readonly ILogger<MetricCircuitBreaker>? _logger;
        private int _failureCount;
        private DateTime? _openedAt;
        private CircuitState _state = CircuitState.Closed;

        public MetricCircuitBreaker(
            int failureThreshold = 5,
            TimeSpan? openDuration = null,
            ILogger<MetricCircuitBreaker>? logger = null)
        {
            _failureThreshold = failureThreshold;
            _openDuration = openDuration ?? TimeSpan.FromSeconds(30);
            _logger = logger;
        }

        /// <summary>
        /// Ejecuta una operación con circuit breaker
        /// </summary>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            if (_state == CircuitState.Open)
            {
                if (DateTime.UtcNow - _openedAt >= _openDuration)
                {
                    _state = CircuitState.HalfOpen;
                    _logger?.LogInformation("Circuit breaker moving to HalfOpen state");
                }
                else
                {
                    throw new CircuitBreakerOpenException("Circuit breaker is open");
                }
            }

            try
            {
                var result = await operation();
                OnSuccess();
                return result;
            }
            catch (Exception)
            {
                OnFailure();
                throw;
            }
        }

        /// <summary>
        /// Ejecuta una acción con circuit breaker
        /// </summary>
        public async Task ExecuteAsync(Func<Task> operation)
        {
            await ExecuteAsync(async () =>
            {
                await operation();
                return true;
            });
        }

        private void OnSuccess()
        {
            if (_state == CircuitState.HalfOpen)
            {
                _state = CircuitState.Closed;
                _failureCount = 0;
                _openedAt = null;
                _logger?.LogInformation("Circuit breaker closed after successful operation");
            }
            else
            {
                _failureCount = 0;
            }
        }

        private void OnFailure()
        {
            _failureCount++;
            if (_failureCount >= _failureThreshold)
            {
                _state = CircuitState.Open;
                _openedAt = DateTime.UtcNow;
                _logger?.LogWarning("Circuit breaker opened after {FailureCount} failures", _failureCount);
            }
        }

        public CircuitState State => _state;
    }

    /// <summary>
    /// Estado del circuit breaker
    /// </summary>
    public enum CircuitState
    {
        Closed,
        Open,
        HalfOpen
    }
}

