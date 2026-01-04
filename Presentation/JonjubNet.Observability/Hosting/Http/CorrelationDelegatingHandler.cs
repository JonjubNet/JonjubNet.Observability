using JonjubNet.Observability.Hosting.Configuration;
using JonjubNet.Observability.Shared.Context;
using Microsoft.Extensions.Options;

namespace JonjubNet.Observability.Hosting.Http
{
    /// <summary>
    /// DelegatingHandler para agregar headers de correlación automáticamente en llamadas HTTP salientes
    /// Thread-safe, sin overhead innecesario, optimizado para performance
    /// </summary>
    public class CorrelationDelegatingHandler : DelegatingHandler
    {
        private readonly ObservabilityOptions _options;

        public CorrelationDelegatingHandler(IOptions<ObservabilityOptions> options)
        {
            _options = options.Value;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Obtener contexto actual (thread-safe, sin locks)
            var context = ObservabilityContext.Current;

            if (context != null && !string.IsNullOrEmpty(context.CorrelationId))
            {
                // Mejores prácticas: propagar solo CorrelationId como identificador único de transacción
                // CorrelationId es el identificador principal que relaciona Logging, Metrics y Tracing
                request.Headers.TryAddWithoutValidation(
                    _options.Correlation.CorrelationIdHeaderName,
                    context.CorrelationId);

                // W3C Trace Context: usar CorrelationId como trace-id para compatibilidad con estándares
                // Formato: 00-{trace-id}-{parent-id}-{trace-flags}
                var traceId = context.TraceId ?? context.CorrelationId;
                var spanId = context.SpanId ?? "0000000000000000";
                var traceparent = $"00-{traceId}-{spanId}-01";
                request.Headers.TryAddWithoutValidation("traceparent", traceparent);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}

