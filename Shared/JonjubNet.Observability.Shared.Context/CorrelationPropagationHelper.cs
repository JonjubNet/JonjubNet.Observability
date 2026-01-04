using System.Collections.Concurrent;

namespace JonjubNet.Observability.Shared.Context
{
    /// <summary>
    /// Helper para propagación de CorrelationId en diferentes protocolos
    /// Centraliza la lógica común para evitar duplicación de código
    /// Thread-safe, sin overhead innecesario, optimizado para performance
    /// </summary>
    public static class CorrelationPropagationHelper
    {
        // Cache de nombres de headers internados (optimización GC)
        private static readonly ConcurrentDictionary<string, string> _internedHeaderNames = new();

        /// <summary>
        /// Nombre estándar del header para CorrelationId
        /// </summary>
        public const string CorrelationIdHeaderName = "X-Correlation-Id";

        /// <summary>
        /// Obtiene CorrelationId del contexto actual
        /// Optimizado: solo lee contexto si es necesario
        /// </summary>
        public static string? GetCorrelationId()
        {
            return ObservabilityContext.Current?.CorrelationId;
        }

        /// <summary>
        /// Crea un diccionario de headers con CorrelationId
        /// Optimizado: solo crea diccionario si hay CorrelationId
        /// </summary>
        public static Dictionary<string, string>? CreateHeadersWithCorrelationId(string? correlationId = null)
        {
            var id = correlationId ?? GetCorrelationId();
            if (string.IsNullOrEmpty(id))
                return null;

            // Pre-allocar capacidad (optimización: solo 1 header)
            var headers = new Dictionary<string, string>(1)
            {
                { InternHeaderName(CorrelationIdHeaderName), id }
            };

            return headers;
        }

        /// <summary>
        /// Agrega CorrelationId a un diccionario de headers existente
        /// Optimizado: no crea nuevo diccionario si ya existe
        /// </summary>
        public static void AddCorrelationIdToHeaders(Dictionary<string, string> headers, string? correlationId = null)
        {
            if (headers == null)
                return;

            var id = correlationId ?? GetCorrelationId();
            if (!string.IsNullOrEmpty(id))
            {
                headers[InternHeaderName(CorrelationIdHeaderName)] = id;
            }
        }

        /// <summary>
        /// Extrae CorrelationId de un diccionario de headers
        /// Optimizado: búsqueda directa sin LINQ
        /// </summary>
        public static string? ExtractCorrelationIdFromHeaders(Dictionary<string, string>? headers)
        {
            if (headers == null || headers.Count == 0)
                return null;

            // Búsqueda directa (optimización: evitar LINQ)
            if (headers.TryGetValue(CorrelationIdHeaderName, out var correlationId))
                return correlationId;

            // Búsqueda case-insensitive (optimización: solo si no se encontró)
            foreach (var kvp in headers)
            {
                if (string.Equals(kvp.Key, CorrelationIdHeaderName, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }

            return null;
        }

        /// <summary>
        /// String interning para nombres de headers comunes (optimización GC)
        /// </summary>
        private static string InternHeaderName(string headerName)
        {
            if (string.IsNullOrEmpty(headerName))
                return string.Empty;

            return _internedHeaderNames.GetOrAdd(headerName, name => string.Intern(name));
        }
    }
}

