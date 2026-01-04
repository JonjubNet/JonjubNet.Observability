namespace JonjubNet.Observability.Metrics.Core.Interfaces
{
    /// <summary>
    /// Interfaz para formateadores de métricas
    /// </summary>
    public interface IMetricFormatter
    {
        /// <summary>
        /// Nombre del formato
        /// </summary>
        string Format { get; }

        /// <summary>
        /// Formatea una lista de puntos de métricas
        /// </summary>
        string FormatMetrics(IReadOnlyList<MetricPoint> points);
    }
}

