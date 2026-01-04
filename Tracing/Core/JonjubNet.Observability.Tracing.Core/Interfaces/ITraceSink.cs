namespace JonjubNet.Observability.Tracing.Core.Interfaces
{
    /// <summary>
    /// Interfaz para sinks de tracing (Adapters)
    /// Todos los sinks leen directamente del Registry para máxima performance
    /// Similar a ILogSink e IMetricsSink
    /// </summary>
    public interface ITraceSink
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
        /// Exporta traces desde el Registry (método principal - optimizado)
        /// </summary>
        ValueTask ExportFromRegistryAsync(TraceRegistry registry, CancellationToken cancellationToken = default);
    }
}

