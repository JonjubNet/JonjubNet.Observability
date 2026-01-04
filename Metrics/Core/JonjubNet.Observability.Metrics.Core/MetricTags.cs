using JonjubNet.Observability.Metrics.Core.Utils;

namespace JonjubNet.Observability.Metrics.Core
{
    /// <summary>
    /// Utilidades para manejo de etiquetas de métricas
    /// </summary>
    public static class MetricTags
    {
        /// <summary>
        /// Crea un diccionario de etiquetas desde pares clave-valor usando object pooling
        /// </summary>
        public static Dictionary<string, string> Create(params (string key, string value)[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                // Retornar diccionario vacío del pool
                return CollectionPool.RentDictionary();
            }

            // Obtener diccionario del pool
            var result = CollectionPool.RentDictionary();
            foreach (var (key, value) in tags)
            {
                result[key] = value;
            }
            return result;
        }

        /// <summary>
        /// Combina múltiples diccionarios de etiquetas usando object pooling
        /// </summary>
        public static Dictionary<string, string> Combine(params Dictionary<string, string>[] tagSets)
        {
            // Obtener diccionario del pool
            var result = CollectionPool.RentDictionary();
            foreach (var tags in tagSets)
            {
                if (tags != null)
                {
                    foreach (var kvp in tags)
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Retorna un diccionario de tags al pool (debe llamarse cuando ya no se use)
        /// </summary>
        public static void ReturnToPool(Dictionary<string, string> tags)
        {
            if (tags != null)
            {
                CollectionPool.ReturnDictionary(tags);
            }
        }
    }
}

