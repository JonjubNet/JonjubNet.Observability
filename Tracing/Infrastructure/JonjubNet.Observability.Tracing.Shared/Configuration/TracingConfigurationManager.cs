using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Tracing.Shared.Configuration
{
    /// <summary>
    /// Gestor de configuración de tracing con soporte para hot-reload
    /// Similar a LoggingConfigurationManager pero para traces
    /// </summary>
    public class TracingConfigurationManager
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TracingConfigurationManager>? _logger;
        private TracingOptions? _cachedOptions;
        private DateTime _lastReloadTime = DateTime.MinValue;
        private readonly TimeSpan _reloadInterval = TimeSpan.FromSeconds(5);

        public TracingConfigurationManager(
            IConfiguration configuration,
            ILogger<TracingConfigurationManager>? logger = null)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene las opciones de tracing con cache y hot-reload
        /// </summary>
        public TracingOptions GetOptions()
        {
            var now = DateTime.UtcNow;

            // Recargar si ha pasado el intervalo o si no hay cache
            if (_cachedOptions == null || now - _lastReloadTime >= _reloadInterval)
            {
                _cachedOptions = LoadOptions();
                _lastReloadTime = now;
                _logger?.LogDebug("Tracing configuration reloaded");
            }

            return _cachedOptions;
        }

        /// <summary>
        /// Carga las opciones desde la configuración
        /// </summary>
        private TracingOptions LoadOptions()
        {
            var options = new TracingOptions();

            // Cargar desde sección "Tracing" o "JonjubNet:Tracing"
            var tracingSection = _configuration.GetSection("JonjubNet:Tracing") 
                ?? _configuration.GetSection("Tracing");

            if (tracingSection.Exists())
            {
                tracingSection.Bind(options);
            }
            else
            {
                // Valores por defecto si no hay configuración
                _logger?.LogWarning("No tracing configuration found, using defaults");
            }

            return options;
        }

        /// <summary>
        /// Fuerza la recarga de la configuración
        /// </summary>
        public void Reload()
        {
            _cachedOptions = null;
            _lastReloadTime = DateTime.MinValue;
            _logger?.LogInformation("Tracing configuration reload forced");
        }
    }
}
