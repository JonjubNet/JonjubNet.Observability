namespace JonjubNet.Observability.Metrics.Core
{
    /// <summary>
    /// Representación inmutable de un punto de métrica
    /// </summary>
    public readonly record struct MetricPoint
    {
        // Singleton para diccionario vacío (reutilizable porque es inmutable en uso)
        private static readonly Dictionary<string, string> EmptyTags = new();

        public string Name { get; init; }
        public MetricType Type { get; init; }
        public double Value { get; init; }
        public Dictionary<string, string> Tags { get; init; }
        public DateTime Timestamp { get; init; }

        public MetricPoint(string name, MetricType type, double value, Dictionary<string, string>? tags = null)
        {
            Name = name;
            Type = type;
            Value = value;
            // Usar singleton para diccionarios vacíos (optimización de memoria)
            Tags = tags ?? EmptyTags;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Tipo de métrica
    /// </summary>
    public enum MetricType
    {
        Counter,
        Gauge,
        Histogram,
        Summary,
        Timer
    }
}

