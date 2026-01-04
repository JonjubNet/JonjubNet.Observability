namespace JonjubNet.Observability.Logging.Core.Interfaces
{
    /// <summary>
    /// Interfaz para sinks de logging (Adapters)
    /// Todos los sinks leen directamente del Registry para máxima performance
    /// Similar a IMetricsSink
    /// </summary>
    public interface ILogSink
    {
        /// <summary>
        /// Nombre del sink
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Indica si el sink está habilitado
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Exporta logs desde el Registry (método principal - optimizado)
        /// </summary>
        ValueTask ExportFromRegistryAsync(LogRegistry registry, CancellationToken cancellationToken = default);
    }
}

