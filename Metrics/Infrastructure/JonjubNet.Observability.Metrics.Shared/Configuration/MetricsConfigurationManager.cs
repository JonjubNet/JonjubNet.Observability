using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Metrics.Shared.Configuration
{
    /// <summary>
    /// Administrador de configuración de métricas con hot-reload
    /// 
    /// Nota sobre logging: Este componente utiliza ILogger estándar de Microsoft.Extensions.Logging
    /// para registrar eventos. Si tu proyecto utiliza Jonjub.Logging, puedes configurarlo como
    /// proveedor de logging y todos los eventos de este componente se registrarán a través de él.
    /// </summary>
    public class MetricsConfigurationManager
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MetricsConfigurationManager>? _logger;
        private MetricsOptions? _currentOptions;

        public MetricsConfigurationManager(
            IConfiguration configuration,
            ILogger<MetricsConfigurationManager>? logger = null)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene la configuración actual
        /// </summary>
        public MetricsOptions GetOptions()
        {
            if (_currentOptions == null)
            {
                _currentOptions = new MetricsOptions();
                _configuration.GetSection("Metrics").Bind(_currentOptions);
            }

            return _currentOptions;
        }

        /// <summary>
        /// Recarga la configuración
        /// </summary>
        public void Reload()
        {
            var previousOptions = _currentOptions;
            _currentOptions = null;
            var newOptions = GetOptions();
            
            // Logging de cambio de configuración usando ILogger estándar
            // Si Jonjub.Logging está configurado como proveedor, estos eventos se registrarán allí
            _logger?.LogInformation(
                "[AUDIT] Metrics configuration reloaded. Previous Enabled: {PreviousEnabled}, New Enabled: {NewEnabled}",
                previousOptions?.Enabled ?? false, newOptions.Enabled);
        }
    }
}

