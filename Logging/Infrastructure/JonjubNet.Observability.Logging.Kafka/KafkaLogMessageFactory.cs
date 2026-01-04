using System.Text;
using System.Text.Json;
using JonjubNet.Observability.Logging.Core;
using JonjubNet.Observability.Shared.Utils;

namespace JonjubNet.Observability.Logging.Kafka
{
    /// <summary>
    /// Factory para crear mensajes Kafka a partir de logs
    /// Similar a KafkaMessageFactory de Metrics pero para logs
    /// </summary>
    public static class KafkaLogMessageFactory
    {
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptionsCache.GetDefault();

        /// <summary>
        /// Crea un mensaje Kafka a partir de un log individual
        /// </summary>
        public static string CreateMessage(StructuredLogEntry log)
        {
            var message = new
            {
                timestamp = log.Timestamp.ToUnixTimeMilliseconds(),
                level = log.Level.ToString(),
                category = log.Category.ToString(),
                message = log.Message,
                exception = log.Exception,
                properties = log.Properties,
                tags = log.Tags,
                correlationId = log.CorrelationId,
                requestId = log.RequestId,
                sessionId = log.SessionId,
                userId = log.UserId,
                eventType = log.EventType ?? string.Empty,
                operation = log.Operation,
                durationMs = log.DurationMs
            };

            return JsonSerializer.Serialize(message, JsonOptions);
        }

        /// <summary>
        /// Crea un mensaje Kafka batch a partir de múltiples logs
        /// </summary>
        public static string CreateBatchMessage(IReadOnlyList<StructuredLogEntry> logs)
        {
            var batch = new
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                count = logs.Count,
                logs = logs.Select(log => new
                {
                    timestamp = log.Timestamp.ToUnixTimeMilliseconds(),
                    level = log.Level.ToString(),
                    category = log.Category.ToString(),
                    message = log.Message,
                    exception = log.Exception,
                    properties = log.Properties,
                    tags = log.Tags,
                    correlationId = log.CorrelationId,
                    requestId = log.RequestId,
                    sessionId = log.SessionId,
                    userId = log.UserId,
                    eventType = log.EventType ?? string.Empty,
                    operation = log.Operation,
                    durationMs = log.DurationMs
                }).ToList()
            };

            return JsonSerializer.Serialize(batch, JsonOptions);
        }
    }
}
