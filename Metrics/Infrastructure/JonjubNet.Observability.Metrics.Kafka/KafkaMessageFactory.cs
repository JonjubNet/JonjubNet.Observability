using System.Text.Json;
using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Shared.Utils;

namespace JonjubNet.Observability.Metrics.Kafka
{
    /// <summary>
    /// Factory para crear mensajes de Kafka desde métricas
    /// </summary>
    public class KafkaMessageFactory
    {
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptionsCache.GetDefault();

        /// <summary>
        /// Crea un mensaje JSON desde un punto de métrica
        /// </summary>
        public static string CreateMessage(MetricPoint point)
        {
            var message = new
            {
                name = point.Name,
                type = point.Type.ToString(),
                value = point.Value,
                tags = point.Tags,
                timestamp = point.Timestamp
            };

            return JsonSerializer.Serialize(message, JsonOptions);
        }

        /// <summary>
        /// Crea un batch de mensajes
        /// </summary>
        public static string CreateBatchMessage(IEnumerable<MetricPoint> points)
        {
            var messages = points.Select(CreateMessage).ToList();
            return JsonSerializer.Serialize(messages, JsonOptions);
        }

        /// <summary>
        /// Crea un mensaje desde el Registry (optimizado)
        /// </summary>
        public static string CreateFromRegistry(
            string name,
            string description,
            string type,
            double value,
            Dictionary<string, string>? tags,
            long timestamp)
        {
            var message = new
            {
                name = name,
                description = description,
                type = type,
                value = value,
                tags = tags,
                timestamp = timestamp
            };

            return JsonSerializer.Serialize(message, JsonOptions);
        }
    }
}

