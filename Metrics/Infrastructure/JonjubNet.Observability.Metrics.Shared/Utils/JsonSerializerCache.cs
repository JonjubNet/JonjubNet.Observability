using System.Text.Json;

namespace JonjubNet.Observability.Metrics.Shared.Utils
{
    /// <summary>
    /// Cache de JsonSerializerOptions para evitar recrearlos en cada serialización
    /// </summary>
    public static class JsonSerializerCache
    {
        private static readonly JsonSerializerOptions DefaultOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly JsonSerializerOptions IndentedOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Obtiene opciones de serialización por defecto (cached)
        /// </summary>
        public static JsonSerializerOptions GetDefaultOptions() => DefaultOptions;

        /// <summary>
        /// Obtiene opciones de serialización con indentación (cached)
        /// </summary>
        public static JsonSerializerOptions GetIndentedOptions() => IndentedOptions;

        /// <summary>
        /// Crea opciones personalizadas (no cached - para casos específicos)
        /// </summary>
        public static JsonSerializerOptions CreateCustomOptions(Action<JsonSerializerOptions>? configure = null)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            configure?.Invoke(options);
            return options;
        }
    }
}

