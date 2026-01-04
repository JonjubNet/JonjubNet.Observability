using Confluent.Kafka;
using JonjubNet.Observability.Shared.Context;

namespace JonjubNet.Observability.Shared.Kafka
{
    /// <summary>
    /// Helper para propagación de CorrelationId en mensajes Kafka
    /// Thread-safe, sin overhead innecesario, optimizado para performance
    /// </summary>
    public static class KafkaCorrelationHelper
    {
        /// <summary>
        /// Crea headers de Kafka con CorrelationId del contexto actual
        /// Optimizado: solo crea headers si hay CorrelationId disponible
        /// </summary>
        public static Headers? CreateKafkaHeaders()
        {
            var correlationId = CorrelationPropagationHelper.GetCorrelationId();
            if (string.IsNullOrEmpty(correlationId))
                return null;

            var headers = new Headers();
            var headerName = CorrelationPropagationHelper.CorrelationIdHeaderName;
            headers.Add(headerName, System.Text.Encoding.UTF8.GetBytes(correlationId));
            return headers;
        }

        /// <summary>
        /// Crea headers de Kafka con CorrelationId específico
        /// </summary>
        public static Headers? CreateKafkaHeaders(string correlationId)
        {
            if (string.IsNullOrEmpty(correlationId))
                return null;

            var headers = new Headers();
            var headerName = CorrelationPropagationHelper.CorrelationIdHeaderName;
            headers.Add(headerName, System.Text.Encoding.UTF8.GetBytes(correlationId));
            return headers;
        }

        /// <summary>
        /// Agrega CorrelationId a headers de Kafka existentes
        /// </summary>
        public static void AddCorrelationIdToHeaders(Headers headers, string? correlationId = null)
        {
            if (headers == null)
                return;

            var id = correlationId ?? CorrelationPropagationHelper.GetCorrelationId();
            if (!string.IsNullOrEmpty(id))
            {
                var headerName = CorrelationPropagationHelper.CorrelationIdHeaderName;
                headers.Add(headerName, System.Text.Encoding.UTF8.GetBytes(id));
            }
        }

        /// <summary>
        /// Extrae CorrelationId de headers de Kafka
        /// Optimizado: búsqueda directa sin LINQ
        /// </summary>
        public static string? ExtractCorrelationIdFromHeaders(Headers? headers)
        {
            if (headers == null || headers.Count == 0)
                return null;

            var headerName = CorrelationPropagationHelper.CorrelationIdHeaderName;
            
            // Búsqueda directa (optimización: evitar LINQ)
            foreach (var header in headers)
            {
                if (header.Key == headerName)
                {
                    return System.Text.Encoding.UTF8.GetString(header.GetValueBytes());
                }
            }

            // Búsqueda case-insensitive (optimización: solo si no se encontró)
            foreach (var header in headers)
            {
                if (string.Equals(header.Key, headerName, StringComparison.OrdinalIgnoreCase))
                {
                    return System.Text.Encoding.UTF8.GetString(header.GetValueBytes());
                }
            }

            return null;
        }
    }
}

