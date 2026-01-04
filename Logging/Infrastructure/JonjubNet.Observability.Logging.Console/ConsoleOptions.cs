namespace JonjubNet.Observability.Logging.Console
{
    /// <summary>
    /// Opciones de configuración para el sink de Console
    /// </summary>
    public class ConsoleOptions
    {
        /// <summary>
        /// Indica si el sink está habilitado
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Formato de salida: "json" o "text"
        /// </summary>
        public string Format { get; set; } = "json";

        /// <summary>
        /// Indica si se debe usar colores en la salida (solo para formato text)
        /// </summary>
        public bool UseColors { get; set; } = true;

        /// <summary>
        /// Nivel mínimo de log a exportar
        /// </summary>
        public string MinLevel { get; set; } = "Trace";
    }
}

