namespace JonjubNet.Observability.Metrics.Core.Interfaces
{
    /// <summary>
    /// Interfaz para sinks de métricas (Adapters)
    /// Todos los sinks ahora leen directamente del Registry para máxima performance
    /// </summary>
    public interface IMetricsSink
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
        /// Exporta métricas desde el Registry (método principal - optimizado)
        /// </summary>
        ValueTask ExportFromRegistryAsync(MetricRegistry registry, CancellationToken cancellationToken = default);
    }
}

