using System.Threading;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Logging.Core.Resilience
{
    /// <summary>
    /// Circuit Breaker para logs
    /// Similar a MetricCircuitBreaker pero para logs
    /// </summary>
    public class LogCircuitBreaker
    {
        private readonly int _failureThreshold;
        private readonly TimeSpan _openDuration;
        private readonly ILogger<LogCircuitBreaker>? _logger;
        private int _failureCount;
        private DateTime? _openedAt;
        private volatile CircuitState _state = CircuitState.Closed;

        public LogCircuitBreaker(
            int failureThreshold = 5,
            TimeSpan? openDuration = null,
            ILogger<LogCircuitBreaker>? logger = null)
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
            var currentState = _state;
            if (currentState == CircuitState.HalfOpen)
            {
                _state = CircuitState.Closed;
                Interlocked.Exchange(ref _failureCount, 0);
                _openedAt = null;
                _logger?.LogInformation("Circuit breaker closed after successful operation");
            }
            else
            {
                Interlocked.Exchange(ref _failureCount, 0);
            }
        }

        private void OnFailure()
        {
            var newCount = Interlocked.Increment(ref _failureCount);
            if (newCount >= _failureThreshold)
            {
                _state = CircuitState.Open;
                _openedAt = DateTime.UtcNow;
                _logger?.LogWarning("Circuit breaker opened after {FailureCount} failures", newCount);
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

