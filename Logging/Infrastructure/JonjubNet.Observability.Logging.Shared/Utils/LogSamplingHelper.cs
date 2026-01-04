using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Logging.Shared.Utils
{
    /// <summary>
    /// Helper para sampling de logs
    /// Implementa sampling probabilístico y rate limiting
    /// </summary>
    public class LogSamplingHelper
    {
        private readonly Random _random = new();
        private readonly ConcurrentDictionary<string, DateTime> _lastLogTime = new();
        private readonly ILogger<LogSamplingHelper>? _logger;
        private readonly double _probability;
        private readonly int _rateLimitPerSecond;
        private readonly bool _enabled;

        public LogSamplingHelper(
            double probability = 1.0,
            int rateLimitPerSecond = 1000,
            bool enabled = false,
            ILogger<LogSamplingHelper>? logger = null)
        {
            _probability = Math.Clamp(probability, 0.0, 1.0);
            _rateLimitPerSecond = Math.Max(1, rateLimitPerSecond);
            _enabled = enabled;
            _logger = logger;
        }

        /// <summary>
        /// Determina si un log debe ser muestreado (sampled)
        /// </summary>
        public bool ShouldSample(string? logKey = null)
        {
            if (!_enabled)
                return true; // Si está deshabilitado, muestrear todos

            // Sampling probabilístico
            if (_random.NextDouble() > _probability)
            {
                return false;
            }

            // Rate limiting por clave (opcional)
            if (!string.IsNullOrEmpty(logKey))
            {
                var now = DateTime.UtcNow;
                var lastTime = _lastLogTime.GetOrAdd(logKey, now);
                var timeSinceLastLog = (now - lastTime).TotalSeconds;

                if (timeSinceLastLog < 1.0 / _rateLimitPerSecond)
                {
                    return false; // Rate limit excedido
                }

                _lastLogTime[logKey] = now;
            }

            return true;
        }

        /// <summary>
        /// Limpia el cache de rate limiting (para evitar memory leaks)
        /// </summary>
        public void CleanupRateLimitCache()
        {
            var now = DateTime.UtcNow;
            var keysToRemove = new List<string>();

            foreach (var kvp in _lastLogTime)
            {
                // Remover entradas más antiguas de 1 minuto
                if ((now - kvp.Value).TotalMinutes > 1)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _lastLogTime.TryRemove(key, out _);
            }
        }
    }
}

