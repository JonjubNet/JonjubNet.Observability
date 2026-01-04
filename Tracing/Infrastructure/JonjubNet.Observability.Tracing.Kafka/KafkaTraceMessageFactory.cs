using System.Text.Json;
using JonjubNet.Observability.Tracing.Core;
using JonjubNet.Observability.Shared.Utils;

namespace JonjubNet.Observability.Tracing.Kafka
{
    /// <summary>
    /// Factory para crear mensajes Kafka a partir de spans
    /// Similar a KafkaLogMessageFactory pero para traces/spans
    /// </summary>
    public static class KafkaTraceMessageFactory
    {
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptionsCache.GetDefault();

        /// <summary>
        /// Crea un mensaje Kafka a partir de un span individual
        /// </summary>
        public static string CreateMessage(Span span)
        {
            var message = new
            {
                spanId = span.SpanId,
                traceId = span.TraceId,
                parentSpanId = span.ParentSpanId,
                operationName = span.OperationName,
                kind = span.Kind.ToString(),
                status = span.Status.ToString(),
                errorMessage = span.ErrorMessage,
                startTime = span.StartTime.ToUnixTimeMilliseconds(),
                endTime = span.EndTime?.ToUnixTimeMilliseconds(),
                durationMs = span.DurationMs,
                tags = span.Tags,
                properties = span.Properties,
                serviceName = span.ServiceName,
                resourceName = span.ResourceName,
                isActive = span.IsActive,
                events = span.Events?.Select(e => new
                {
                    name = e.Name,
                    timestamp = e.Timestamp.ToUnixTimeMilliseconds(),
                    attributes = e.Attributes
                }).ToList()
            };

            return JsonSerializer.Serialize(message, JsonOptions);
        }

        /// <summary>
        /// Crea un mensaje Kafka batch a partir de múltiples spans
        /// Agrupa spans por traceId para mantener la relación
        /// </summary>
        public static string CreateBatchMessage(IReadOnlyList<Span> spans)
        {
            // Agrupar spans por traceId
            var tracesByTraceId = spans
                .GroupBy(s => s.TraceId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var batch = new
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                count = spans.Count,
                traceCount = tracesByTraceId.Count,
                traces = tracesByTraceId.Select(traceGroup => new
                {
                    traceId = traceGroup.Key,
                    spanCount = traceGroup.Value.Count,
                    spans = traceGroup.Value.Select(span => new
                    {
                        spanId = span.SpanId,
                        parentSpanId = span.ParentSpanId,
                        operationName = span.OperationName,
                        kind = span.Kind.ToString(),
                        status = span.Status.ToString(),
                        errorMessage = span.ErrorMessage,
                        startTime = span.StartTime.ToUnixTimeMilliseconds(),
                        endTime = span.EndTime?.ToUnixTimeMilliseconds(),
                        durationMs = span.DurationMs,
                        tags = span.Tags,
                        properties = span.Properties,
                        serviceName = span.ServiceName,
                        resourceName = span.ResourceName,
                        isActive = span.IsActive,
                        events = span.Events?.Select(e => new
                        {
                            name = e.Name,
                            timestamp = e.Timestamp.ToUnixTimeMilliseconds(),
                            attributes = e.Attributes
                        }).ToList()
                    }).ToList()
                }).ToList()
            };

            return JsonSerializer.Serialize(batch, JsonOptions);
        }
    }
}

