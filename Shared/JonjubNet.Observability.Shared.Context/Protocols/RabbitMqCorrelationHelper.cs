using System.Collections.Generic;

namespace JonjubNet.Observability.Shared.Context.Protocols
{
    /// <summary>
    /// Helper para propagación de CorrelationId en RabbitMQ
    /// Thread-safe, sin overhead innecesario, optimizado para performance
    /// </summary>
    public static class RabbitMqCorrelationHelper
    {
        /// <summary>
        /// Crea BasicProperties de RabbitMQ con CorrelationId del contexto actual
        /// Optimizado: solo crea properties si hay CorrelationId disponible
        /// </summary>
        public static Dictionary<string, object>? CreateProperties()
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
        /// Agrega CorrelationId a BasicProperties de RabbitMQ existente
        /// </summary>
        public static void AddCorrelationIdToProperties(IDictionary<string, object> properties, string? correlationId = null)
        {
            if (properties == null)
                return;

            var id = correlationId ?? CorrelationPropagationHelper.GetCorrelationId();
            if (!string.IsNullOrEmpty(id))
            {
                properties[CorrelationPropagationHelper.CorrelationIdHeaderName] = id;
            }
        }

        /// <summary>
        /// Extrae CorrelationId de BasicProperties de RabbitMQ
        /// Optimizado: búsqueda directa sin LINQ
        /// </summary>
        public static string? ExtractCorrelationIdFromProperties(IDictionary<string, object>? properties)
        {
            if (properties == null || properties.Count == 0)
                return null;

            var headerName = CorrelationPropagationHelper.CorrelationIdHeaderName;

            // Búsqueda directa (optimización: evitar LINQ)
            if (properties.TryGetValue(headerName, out var value))
            {
                return value?.ToString();
            }

            // Búsqueda case-insensitive (optimización: solo si no se encontró)
            foreach (var kvp in properties)
            {
                if (string.Equals(kvp.Key, headerName, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value?.ToString();
                }
            }

            return null;
        }
    }
}

