using System.Collections.Concurrent;
using JonjubNet.Observability.Logging.Core.Interfaces;
using JonjubNet.Observability.Logging.Core.Resilience;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Logging.Shared.Resilience
{
    /// <summary>
    /// Gestor de circuit breakers por sink individual
    /// Permite tener un circuit breaker independiente para cada sink
    /// Similar a Metrics.Shared.Resilience.SinkCircuitBreakerManager pero para logs
    /// </summary>
    public class SinkCircuitBreakerManager : ISinkCircuitBreakerManager
    {
        private readonly ConcurrentDictionary<string, LogCircuitBreaker> _circuitBreakers = new();
        private readonly ILogger<SinkCircuitBreakerManager>? _logger;
        private readonly CircuitBreakerOptions _defaultOptions;
        private readonly Dictionary<string, CircuitBreakerOptions> _sinkSpecificOptions;
        private readonly bool _enabled;

        public SinkCircuitBreakerManager(
            CircuitBreakerOptions? defaultOptions = null,
            Dictionary<string, CircuitBreakerOptions>? sinkSpecificOptions = null,
            ILogger<SinkCircuitBreakerManager>? logger = null,
            bool enabled = true)
        {
            _defaultOptions = defaultOptions ?? new CircuitBreakerOptions();
            _sinkSpecificOptions = sinkSpecificOptions ?? new Dictionary<string, CircuitBreakerOptions>();
            _logger = logger;
            _enabled = enabled;
        }

        /// <summary>
        /// Obtiene o crea un circuit breaker para un sink específico
        /// </summary>
        public LogCircuitBreaker? GetOrCreateCircuitBreaker(ILogSink sink)
        {
            if (!_enabled)
                return null;

            var sinkName = sink.Name;
            
            // Si ya existe, retornarlo
            if (_circuitBreakers.TryGetValue(sinkName, out var existing))
            {
                return existing;
            }

            // Obtener opciones específicas del sink o usar las por defecto
            var options = _sinkSpecificOptions.TryGetValue(sinkName, out var specificOptions)
                ? specificOptions
                : _defaultOptions;

            // Crear nuevo circuit breaker
            // Nota: LogCircuitBreaker espera ILogger<LogCircuitBreaker>, pero tenemos ILogger<SinkCircuitBreakerManager>
            // Por ahora pasamos null, el circuit breaker puede funcionar sin logger
            var circuitBreaker = new LogCircuitBreaker(
                failureThreshold: options.FailureThreshold,
                openDuration: TimeSpan.FromSeconds(options.OpenDurationSeconds),
                logger: null);

            // Intentar agregar (puede fallar si otro thread lo creó primero)
            _circuitBreakers.TryAdd(sinkName, circuitBreaker);
            
            // Retornar el que está en el diccionario (puede ser el que acabamos de crear o uno existente)
            return _circuitBreakers.TryGetValue(sinkName, out var result) ? result : circuitBreaker;
        }

        /// <summary>
        /// Ejecuta una operación con circuit breaker del sink
        /// </summary>
        public async Task<T> ExecuteWithCircuitBreakerAsync<T>(
            ILogSink sink,
            Func<Task<T>> operation)
        {
            if (!_enabled)
            {
                return await operation();
            }

            var circuitBreaker = GetOrCreateCircuitBreaker(sink);
            
            if (circuitBreaker == null)
            {
                return await operation();
            }
            
            try
            {
                return await circuitBreaker.ExecuteAsync(operation);
            }
            catch (CircuitBreakerOpenException)
            {
                _logger?.LogWarning(
                    "Circuit breaker is open for sink {SinkName}. Operation skipped. State: {State}",
                    sink.Name, circuitBreaker.State);
                throw;
            }
        }

        /// <summary>
        /// Ejecuta una acción con circuit breaker del sink
        /// </summary>
        public async Task ExecuteWithCircuitBreakerAsync(
            ILogSink sink,
            Func<Task> operation)
        {
            await ExecuteWithCircuitBreakerAsync(sink, async () =>
            {
                await operation();
                return true;
            });
        }

        /// <summary>
        /// Resetea el circuit breaker de un sink específico
        /// </summary>
        public void ResetCircuitBreaker(string sinkName)
        {
            if (_circuitBreakers.TryRemove(sinkName, out _))
            {
                _logger?.LogInformation("Circuit breaker reset for sink {SinkName}", sinkName);
            }
        }

        /// <summary>
        /// Resetea todos los circuit breakers
        /// </summary>
        public void ResetAllCircuitBreakers()
        {
            _circuitBreakers.Clear();
            _logger?.LogInformation("All circuit breakers reset");
        }
    }

    /// <summary>
    /// Opciones de configuración para circuit breakers
    /// </summary>
    public class CircuitBreakerOptions
    {
        /// <summary>
        /// Número de fallos antes de abrir el circuit breaker
        /// </summary>
        public int FailureThreshold { get; set; } = 5;

        /// <summary>
        /// Duración que el circuit breaker permanece abierto (en segundos)
        /// </summary>
        public int OpenDurationSeconds { get; set; } = 30;
    }
}

