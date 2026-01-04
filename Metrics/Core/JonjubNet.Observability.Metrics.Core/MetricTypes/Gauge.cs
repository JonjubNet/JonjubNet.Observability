using System.Collections.Concurrent;
using JonjubNet.Observability.Metrics.Core.Utils;

namespace JonjubNet.Observability.Metrics.Core.MetricTypes
{
    /// <summary>
    /// Gauge que puede subir o bajar
    /// </summary>
    public class Gauge
    {
        private readonly ConcurrentDictionary<string, double> _gauges = new();
        private readonly string _name;
        private readonly string _description;

        public Gauge(string name, string description)
        {
            _name = name;
            _description = description;
        }

        public string Name => _name;
        public string Description => _description;

        /// <summary>
        /// Establece el valor del gauge
        /// </summary>
        public void Set(Dictionary<string, string>? tags = null, double value = 0.0)
        {
            var key = KeyCache.CreateKey(tags);
            _gauges.AddOrUpdate(key, value, (k, v) => value);
        }

        /// <summary>
        /// Incrementa el gauge
        /// </summary>
        public void Inc(Dictionary<string, string>? tags = null, double value = 1.0)
        {
            var key = KeyCache.CreateKey(tags);
            _gauges.AddOrUpdate(key, value, (k, v) => v + value);
        }

        /// <summary>
        /// Decrementa el gauge
        /// </summary>
        public void Dec(Dictionary<string, string>? tags = null, double value = 1.0)
        {
            var key = KeyCache.CreateKey(tags);
            _gauges.AddOrUpdate(key, -value, (k, v) => v - value);
        }

        /// <summary>
        /// Obtiene el valor actual del gauge
        /// </summary>
        public double GetValue(Dictionary<string, string>? tags = null)
        {
            var key = KeyCache.CreateKey(tags);
            return _gauges.GetValueOrDefault(key, 0.0);
        }

        /// <summary>
        /// Obtiene todos los valores del gauge (sin copia, retorna referencia directa)
        /// </summary>
        public IReadOnlyDictionary<string, double> GetAllValues()
        {
            return _gauges; // Retornar directamente sin copia
        }
    }
}

