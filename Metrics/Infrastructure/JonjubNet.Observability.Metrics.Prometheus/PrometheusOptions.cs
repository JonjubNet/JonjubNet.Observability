namespace JonjubNet.Observability.Metrics.Prometheus
{
    /// <summary>
    /// Opciones de configuración para Prometheus
    /// </summary>
    public class PrometheusOptions
    {
        /// <summary>
        /// Ruta del endpoint (default: /metrics)
        /// </summary>
        public string Path { get; set; } = "/metrics";

        /// <summary>
        /// Puerto del endpoint (opcional, si se especifica se crea servidor separado)
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// Habilitar el exporter
        /// Por defecto está habilitado (true) - Prometheus es el sink más común
        /// Puede deshabilitarse estableciendo Enabled: false en la configuración
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}

