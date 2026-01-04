namespace JonjubNet.Observability.Metrics.StatsD
{
    /// <summary>
    /// Opciones de configuración para StatsD
    /// </summary>
    public class StatsDOptions
    {
        /// <summary>
        /// Host de StatsD
        /// </summary>
        public string Host { get; set; } = "localhost";

        /// <summary>
        /// Puerto de StatsD
        /// </summary>
        public int Port { get; set; } = 8125;

        /// <summary>
        /// Habilitar el exporter
        /// </summary>
        public bool Enabled { get; set; } = false;
    }
}

