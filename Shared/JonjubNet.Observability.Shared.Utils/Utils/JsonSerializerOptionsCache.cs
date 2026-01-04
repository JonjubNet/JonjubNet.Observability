using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JonjubNet.Observability.Shared.Utils
{
    /// <summary>
    /// Cache de JsonSerializerOptions para evitar recrearlos en cada serialización
    /// Común para Metrics y Logging
    /// </summary>
    public static class JsonSerializerOptionsCache
    {
        private static readonly ConcurrentDictionary<string, JsonSerializerOptions> _cache = new();

        /// <summary>
        /// Obtiene o crea JsonSerializerOptions con configuración estándar
        /// </summary>
        public static JsonSerializerOptions GetDefault()
        {
            return _cache.GetOrAdd("default", _ => new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false,
                PropertyNameCaseInsensitive = true
            });
        }

        /// <summary>
        /// Obtiene o crea JsonSerializerOptions con configuración personalizada
        /// </summary>
        public static JsonSerializerOptions GetOrCreate(string key, Func<JsonSerializerOptions> factory)
        {
            return _cache.GetOrAdd(key, _ => factory());
        }

        /// <summary>
        /// Limpia el cache
        /// </summary>
        public static void Clear()
        {
            _cache.Clear();
        }
    }
}

