using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Tracing.Core.Resilience
{
    /// <summary>
    /// Política de reintentos con backoff exponencial y jitter
    /// Similar a Logging.Core.Resilience.RetryPolicy pero para traces
    /// </summary>
    public class RetryPolicy
    {
        private readonly int _maxRetries;
        private readonly TimeSpan _initialDelay;
        private readonly double _backoffMultiplier;
        private readonly double _jitterPercent;
        private readonly Random _random;
        private readonly ILogger<RetryPolicy>? _logger;

        public RetryPolicy(
            int maxRetries = 3,
            TimeSpan? initialDelay = null,
            double backoffMultiplier = 2.0,
            double jitterPercent = 0.1, // 10% de jitter por defecto
            ILogger<RetryPolicy>? logger = null)
        {
            _maxRetries = maxRetries;
            _initialDelay = initialDelay ?? TimeSpan.FromMilliseconds(100);
            _backoffMultiplier = backoffMultiplier;
            _jitterPercent = Math.Clamp(jitterPercent, 0.0, 1.0); // Entre 0% y 100%
            _random = new Random();
            _logger = logger;
        }

        /// <summary>
        /// Calcula el delay con exponential backoff y jitter
        /// </summary>
        private TimeSpan CalculateDelay(int attempt)
        {
            // Exponential backoff: initialDelay * (backoffMultiplier ^ attempt)
            var baseDelay = TimeSpan.FromMilliseconds(
                _initialDelay.TotalMilliseconds * Math.Pow(_backoffMultiplier, attempt));

            // Agregar jitter aleatorio (±jitterPercent)
            var jitterRange = baseDelay.TotalMilliseconds * _jitterPercent;
            var jitter = (_random.NextDouble() * 2 - 1) * jitterRange; // Entre -jitterRange y +jitterRange

            var finalDelay = baseDelay.TotalMilliseconds + jitter;
            
            // Asegurar que el delay no sea negativo
            return TimeSpan.FromMilliseconds(Math.Max(0, finalDelay));
        }

        /// <summary>
        /// Ejecuta una función con reintentos y exponential backoff con jitter
        /// </summary>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
        {
            Exception? lastException = null;

            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (attempt < _maxRetries)
                    {
                        var delay = CalculateDelay(attempt);
                        _logger?.LogWarning(
                            ex,
                            "Retry attempt {Attempt} of {MaxRetries} after {Delay}ms (with jitter)",
                            attempt + 1,
                            _maxRetries,
                            delay.TotalMilliseconds);
                        await Task.Delay(delay, cancellationToken);
                    }
                }
            }

            _logger?.LogError(lastException, "All retry attempts exhausted after {MaxRetries} retries", _maxRetries);
            throw lastException ?? new InvalidOperationException("Operation failed after all retries");
        }

        /// <summary>
        /// Ejecuta una acción con reintentos y exponential backoff con jitter
        /// </summary>
        public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            await ExecuteAsync(async () =>
            {
                await operation();
                return true;
            }, cancellationToken);
        }

        /// <summary>
        /// Ejecuta una operación con reintentos y retorna información sobre el resultado
        /// </summary>
        public async Task<RetryResult<T>> ExecuteWithResultAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            Exception? lastException = null;
            var attempts = new List<RetryAttempt>(_maxRetries + 1);

            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                var attemptStart = DateTime.UtcNow;
                try
                {
                    var result = await operation();
                    var attemptDuration = DateTime.UtcNow - attemptStart;
                    
                    attempts.Add(new RetryAttempt
                    {
                        AttemptNumber = attempt + 1,
                        Success = true,
                        Duration = attemptDuration,
                        Exception = null
                    });

                    return new RetryResult<T>
                    {
                        Value = result,
                        Success = true,
                        Attempts = attempts,
                        TotalAttempts = attempt + 1
                    };
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    var attemptDuration = DateTime.UtcNow - attemptStart;
                    var delay = attempt < _maxRetries ? CalculateDelay(attempt) : TimeSpan.Zero;

                    attempts.Add(new RetryAttempt
                    {
                        AttemptNumber = attempt + 1,
                        Success = false,
                        Duration = attemptDuration,
                        Exception = ex,
                        NextRetryDelay = delay
                    });

                    if (attempt < _maxRetries)
                    {
                        await Task.Delay(delay, cancellationToken);
                    }
                }
            }

            return new RetryResult<T>
            {
                Value = default!,
                Success = false,
                Attempts = attempts,
                TotalAttempts = _maxRetries + 1,
                LastException = lastException
            };
        }
    }

    /// <summary>
    /// Resultado de una operación con reintentos
    /// </summary>
    public class RetryResult<T>
    {
        public T Value { get; set; } = default!;
        public bool Success { get; set; }
        public List<RetryAttempt> Attempts { get; set; } = new();
        public int TotalAttempts { get; set; }
        public Exception? LastException { get; set; }
    }

    /// <summary>
    /// Información sobre un intento de reintento
    /// </summary>
    public class RetryAttempt
    {
        public int AttemptNumber { get; set; }
        public bool Success { get; set; }
        public TimeSpan Duration { get; set; }
        public Exception? Exception { get; set; }
        public TimeSpan NextRetryDelay { get; set; }
    }
}
