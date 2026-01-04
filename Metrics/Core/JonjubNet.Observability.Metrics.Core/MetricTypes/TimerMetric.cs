using System.Diagnostics;

namespace JonjubNet.Observability.Metrics.Core.MetricTypes
{
    /// <summary>
    /// Timer para medir duración de operaciones
    /// </summary>
    public class TimerMetric : IDisposable
    {
        private readonly Histogram _histogram;
        private readonly Dictionary<string, string>? _tags;
        private readonly Stopwatch _stopwatch;

        public TimerMetric(Histogram histogram, Dictionary<string, string>? tags = null)
        {
            _histogram = histogram;
            _tags = tags;
            _stopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Detiene el timer y registra la duración
        /// </summary>
        public void Stop()
        {
            _stopwatch.Stop();
            var durationSeconds = _stopwatch.Elapsed.TotalSeconds;
            _histogram.Observe(_tags, durationSeconds);
        }

        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Crea un timer y lo inicia
        /// </summary>
        public static TimerMetric Start(Histogram histogram, Dictionary<string, string>? tags = null)
        {
            return new TimerMetric(histogram, tags);
        }
    }
}

