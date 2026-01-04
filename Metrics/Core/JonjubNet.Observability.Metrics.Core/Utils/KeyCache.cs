using System.Collections.Concurrent;
using System.Text;

namespace JonjubNet.Observability.Metrics.Core.Utils
{
    /// <summary>
    /// Cache de keys generadas para tags de métricas
    /// Reduce allocations en CreateKey() usando cache y StringBuilder
    /// </summary>
    public static class KeyCache
    {
        private static readonly ConcurrentDictionary<string, string> _cache = new();
        private const int MaxCacheSize = 10000;

        /// <summary>
        /// Crea una key para tags usando cache y StringBuilder optimizado
        /// </summary>
        public static string CreateKey(Dictionary<string, string>? tags)
        {
            if (tags == null || tags.Count == 0)
                return string.Empty;

            // Crear key temporal para lookup
            var tempKey = BuildKeyFast(tags);
            
            // Intentar obtener del cache
            if (_cache.TryGetValue(tempKey, out var cachedKey))
            {
                return cachedKey;
            }

            // Si no está en cache, crear y agregar (con límite de tamaño)
            if (_cache.Count < MaxCacheSize)
            {
                var newKey = BuildKeyFast(tags);
                _cache.TryAdd(newKey, newKey);
                return newKey;
            }

            // Si el cache está lleno, retornar key sin cachear
            return tempKey;
        }

        /// <summary>
        /// Construye la key usando StringBuilder para evitar allocations intermedias
        /// </summary>
        private static string BuildKeyFast(Dictionary<string, string> tags)
        {
            if (tags.Count == 0)
                return string.Empty;

            // Usar StringBuilder con capacidad pre-calculada
            var sb = new StringBuilder(tags.Count * 16); // Estimación: ~16 chars por tag
            
            var first = true;
            foreach (var kvp in tags.OrderBy(x => x.Key))
            {
                if (!first)
                    sb.Append(',');
                
                sb.Append(kvp.Key);
                sb.Append('=');
                sb.Append(kvp.Value);
                first = false;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Limpia el cache (útil para testing o cuando se necesita liberar memoria)
        /// </summary>
        public static void Clear()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Obtiene el tamaño actual del cache
        /// </summary>
        public static int Count => _cache.Count;
    }
}

