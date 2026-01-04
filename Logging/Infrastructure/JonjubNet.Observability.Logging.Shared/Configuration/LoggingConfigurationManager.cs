using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JonjubNet.Observability.Logging.Shared.Configuration
{
    /// <summary>
    /// Gestor de configuración de logging con soporte para hot-reload
    /// Similar a MetricsConfigurationManager pero para logs
    /// </summary>
    public class LoggingConfigurationManager
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoggingConfigurationManager>? _logger;
        private LoggingOptions? _cachedOptions;
        private DateTime _lastReloadTime = DateTime.MinValue;
        private readonly TimeSpan _reloadInterval = TimeSpan.FromSeconds(5);

        public LoggingConfigurationManager(
            IConfiguration configuration,
            ILogger<LoggingConfigurationManager>? logger = null)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene las opciones de logging con cache y hot-reload
        /// </summary>
        public LoggingOptions GetOptions()
        {
            var now = DateTime.UtcNow;

            // Recargar si ha pasado el intervalo o si no hay cache
            if (_cachedOptions == null || now - _lastReloadTime >= _reloadInterval)
            {
                _cachedOptions = LoadOptions();
                _lastReloadTime = now;
                _logger?.LogDebug("Logging configuration reloaded");
            }

            return _cachedOptions;
        }

        /// <summary>
        /// Carga las opciones desde la configuración
        /// </summary>
        private LoggingOptions LoadOptions()
        {
            var options = new LoggingOptions();

            // Cargar desde sección "Logging" o "JonjubNet:Logging"
            var loggingSection = _configuration.GetSection("JonjubNet:Logging") 
                ?? _configuration.GetSection("Logging");

            if (loggingSection.Exists())
            {
                loggingSection.Bind(options);
            }
            else
            {
                // Valores por defecto si no hay configuración
                _logger?.LogWarning("No logging configuration found, using defaults");
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
            _logger?.LogInformation("Logging configuration reload forced");
        }
    }
}

