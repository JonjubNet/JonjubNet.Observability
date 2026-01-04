using System.Collections.Concurrent;
using JonjubNet.Observability.Metrics.Core.Interfaces;
using JonjubNet.Observability.Metrics.Core.Resilience;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Metrics.Shared.Resilience
{
    /// <summary>
    /// Gestor de circuit breakers por sink individual
    /// Permite tener un circuit breaker independiente para cada sink
    /// </summary>
    public class SinkCircuitBreakerManager : ISinkCircuitBreakerManager
    {
        private readonly ConcurrentDictionary<string, MetricCircuitBreaker> _circuitBreakers = new();
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
        public MetricCircuitBreaker? GetOrCreateCircuitBreaker(IMetricsSink sink)
        {
            if (!_enabled)
                return null;

            // Verificar si ya existe
            if (_circuitBreakers.TryGetValue(sink.Name, out var existing))
            {
                return existing;
            }

            // Usar opciones específicas del sink si existen, sino usar las por defecto
            var options = _sinkSpecificOptions.GetValueOrDefault(sink.Name, _defaultOptions);
            
            if (!options.Enabled)
                return null;

            // Crear nuevo circuit breaker
            var circuitBreaker = new MetricCircuitBreaker(
                failureThreshold: options.FailureThreshold,
                openDuration: options.OpenDuration,
                logger: null); // Circuit breaker puede funcionar sin logger

            // Intentar agregar (puede fallar si otro thread lo creó primero)
            _circuitBreakers.TryAdd(sink.Name, circuitBreaker);
            
            // Retornar el que está en el diccionario (puede ser el que creamos o uno creado por otro thread)
            return _circuitBreakers.GetValueOrDefault(sink.Name);
        }

        /// <summary>
        /// Obtiene el estado del circuit breaker de un sink
        /// </summary>
        public CircuitState? GetSinkCircuitState(string sinkName)
        {
            if (_circuitBreakers.TryGetValue(sinkName, out var circuitBreaker))
            {
                return circuitBreaker.State;
            }
            return null;
        }

        /// <summary>
        /// Obtiene el estado de todos los circuit breakers
        /// </summary>
        public Dictionary<string, CircuitState> GetAllCircuitStates()
        {
            var result = new Dictionary<string, CircuitState>();
            foreach (var kvp in _circuitBreakers)
            {
                result[kvp.Key] = kvp.Value.State;
            }
            return result;
        }

        /// <summary>
        /// Ejecuta una operación con circuit breaker del sink
        /// </summary>
        public async Task<T> ExecuteWithCircuitBreakerAsync<T>(
            IMetricsSink sink,
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
            IMetricsSink sink,
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
        /// Duración que el circuit breaker permanece abierto antes de intentar HalfOpen
        /// </summary>
        public TimeSpan OpenDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Indica si el circuit breaker está habilitado
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}

