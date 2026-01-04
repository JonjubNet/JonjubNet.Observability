using System.Collections.Generic;

namespace JonjubNet.Observability.Shared.Context.Protocols
{
    /// <summary>
    /// Helper para propagación de CorrelationId en Azure Service Bus
    /// Thread-safe, sin overhead innecesario, optimizado para performance
    /// </summary>
    public static class AzureServiceBusCorrelationHelper
    {
        /// <summary>
        /// Crea ApplicationProperties de Azure Service Bus con CorrelationId del contexto actual
        /// Optimizado: solo crea properties si hay CorrelationId disponible
        /// </summary>
        public static Dictionary<string, object>? CreateApplicationProperties()
        {
            var correlationId = CorrelationPropagationHelper.GetCorrelationId();
            if (string.IsNullOrEmpty(correlationId))
                return null;

            // Pre-allocar capacidad (optimización: solo 1 property)
            var properties = new Dictionary<string, object>(1)
            {
                { CorrelationPropagationHelper.CorrelationIdHeaderName, correlationId }
            };

            return properties;
        }

        /// <summary>
        /// Agrega CorrelationId a ApplicationProperties de Azure Service Bus existente
        /// </summary>
        public static void AddCorrelationIdToApplicationProperties(IDictionary<string, object> applicationProperties, string? correlationId = null)
        {
            if (applicationProperties == null)
                return;

            var id = correlationId ?? CorrelationPropagationHelper.GetCorrelationId();
            if (!string.IsNullOrEmpty(id))
            {
                applicationProperties[CorrelationPropagationHelper.CorrelationIdHeaderName] = id;
            }
        }

        /// <summary>
        /// Extrae CorrelationId de ApplicationProperties de Azure Service Bus
        /// Optimizado: búsqueda directa sin LINQ
        /// </summary>
        public static string? ExtractCorrelationIdFromApplicationProperties(IDictionary<string, object>? applicationProperties)
        {
            return RabbitMqCorrelationHelper.ExtractCorrelationIdFromProperties(applicationProperties);
        }
    }
}

