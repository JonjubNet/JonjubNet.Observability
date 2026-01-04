using System.Collections.Generic;

namespace JonjubNet.Observability.Shared.Context.Protocols
{
    /// <summary>
    /// Helper para propagación de CorrelationId en gRPC
    /// Thread-safe, sin overhead innecesario, optimizado para performance
    /// </summary>
    public static class GrpcCorrelationHelper
    {
        /// <summary>
        /// Crea metadata de gRPC con CorrelationId del contexto actual
        /// Optimizado: solo crea metadata si hay CorrelationId disponible
        /// </summary>
        public static Dictionary<string, string>? CreateMetadata()
        {
            var correlationId = CorrelationPropagationHelper.GetCorrelationId();
            if (string.IsNullOrEmpty(correlationId))
                return null;

            // Pre-allocar capacidad (optimización: solo 1 metadata)
            var metadata = new Dictionary<string, string>(1)
            {
                { CorrelationPropagationHelper.CorrelationIdHeaderName, correlationId }
            };

            return metadata;
        }

        /// <summary>
        /// Crea metadata de gRPC con CorrelationId específico
        /// </summary>
        public static Dictionary<string, string>? CreateMetadata(string correlationId)
        {
            if (string.IsNullOrEmpty(correlationId))
                return null;

            var metadata = new Dictionary<string, string>(1)
            {
                { CorrelationPropagationHelper.CorrelationIdHeaderName, correlationId }
            };

            return metadata;
        }

        /// <summary>
        /// Agrega CorrelationId a metadata de gRPC existente
        /// </summary>
        public static void AddCorrelationIdToMetadata(Dictionary<string, string> metadata, string? correlationId = null)
        {
            if (metadata == null)
                return;

            var id = correlationId ?? CorrelationPropagationHelper.GetCorrelationId();
            if (!string.IsNullOrEmpty(id))
            {
                metadata[CorrelationPropagationHelper.CorrelationIdHeaderName] = id;
            }
        }

        /// <summary>
        /// Extrae CorrelationId de metadata de gRPC
        /// Optimizado: búsqueda directa sin LINQ
        /// </summary>
        public static string? ExtractCorrelationIdFromMetadata(Dictionary<string, string>? metadata)
        {
            return CorrelationPropagationHelper.ExtractCorrelationIdFromHeaders(metadata);
        }
    }
}

