namespace JonjubNet.Observability.Logging.Serilog
{
    /// <summary>
    /// Opciones de configuración para el sink de Serilog
    /// </summary>
    public class SerilogOptions
    {
        /// <summary>
        /// Indica si el sink está habilitado
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Configuración adicional de Serilog (opcional)
        /// </summary>
        public Dictionary<string, object>? AdditionalConfiguration { get; set; }
    }
}

