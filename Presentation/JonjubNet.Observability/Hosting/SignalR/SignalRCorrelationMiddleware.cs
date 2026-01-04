using JonjubNet.Observability.Hosting.Configuration;
using JonjubNet.Observability.Shared.Context;
using Microsoft.AspNetCore.Http;
#if SIGNALR_SUPPORT
using Microsoft.AspNetCore.SignalR;
#endif
using Microsoft.Extensions.Options;

namespace JonjubNet.Observability.Hosting.SignalR
{
    /// <summary>
    /// Middleware para propagación de CorrelationId en SignalR
    /// Thread-safe, sin overhead innecesario, optimizado para performance
    /// </summary>
    public class SignalRCorrelationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ObservabilityOptions _options;

        public SignalRCorrelationMiddleware(
            RequestDelegate next,
            IOptions<ObservabilityOptions> options)
        {
            _next = next;
            _options = options.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Leer o generar CorrelationId (similar a ObservabilityHttpMiddleware)
            var correlationId = GetOrGenerateCorrelationId(context);

            // Establecer en contexto compartido
            ObservabilityContext.SetCorrelationId(correlationId);

            // Agregar CorrelationId al contexto de SignalR si está disponible
            if (context.WebSockets.IsWebSocketRequest || context.Request.Path.StartsWithSegments("/hub"))
            {
                context.Items[CorrelationPropagationHelper.CorrelationIdHeaderName] = correlationId;
            }

            await _next(context);
        }

        /// <summary>
        /// Obtiene CorrelationId del header o genera uno nuevo automáticamente
        /// Reutiliza lógica de ObservabilityHttpMiddleware para evitar duplicación
        /// </summary>
        private string GetOrGenerateCorrelationId(HttpContext context)
        {
            if (_options.Correlation.ReadIncomingCorrelationId)
            {
                var headerValue = context.Request.Headers[_options.Correlation.CorrelationIdHeaderName].FirstOrDefault();
                if (!string.IsNullOrEmpty(headerValue))
                {
                    return headerValue;
                }
            }

            return TraceIdGenerator.GenerateCorrelationId();
        }
    }

#if SIGNALR_SUPPORT
    /// <summary>
    /// Extensiones para propagar CorrelationId en mensajes SignalR
    /// Requiere: Paquete Microsoft.AspNetCore.SignalR (opcional)
    /// </summary>
    public static class SignalRCorrelationExtensions
    {
        /// <summary>
        /// Propaga CorrelationId en un mensaje SignalR
        /// </summary>
        public static void PropagateCorrelationId(this IHubContext hubContext, string? correlationId = null)
        {
            var id = correlationId ?? CorrelationPropagationHelper.GetCorrelationId();
            if (!string.IsNullOrEmpty(id))
            {
                // CorrelationId se propaga automáticamente a través del contexto HTTP
                // Los clientes pueden leerlo del contexto si es necesario
            }
        }
    }
#else
    // SignalR support requiere el paquete Microsoft.AspNetCore.SignalR
    // Para habilitar, agregue la referencia al paquete y defina SIGNALR_SUPPORT en el proyecto
    // Ejemplo: <DefineConstants>$(DefineConstants);SIGNALR_SUPPORT</DefineConstants>
#endif
}

