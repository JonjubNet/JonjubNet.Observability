namespace JonjubNet.Observability.Metrics.Shared.Configuration
{
    /// <summary>
    /// Opciones base para sinks
    /// </summary>
    public abstract class MetricsSinkOptions
    {
        /// <summary>
        /// Habilitar el sink
        /// Por defecto está deshabilitado (false) - debe habilitarse explícitamente en la configuración
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Nombre del sink
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}

