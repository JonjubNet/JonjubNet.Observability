using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Logging.Shared.Utils
{
    /// <summary>
    /// Helper para sanitización de datos sensibles en logs
    /// Enmascara información sensible como tarjetas de crédito, SSN, emails, etc.
    /// </summary>
    public class DataSanitizationHelper
    {
        private readonly List<Regex> _sensitivePatterns;
        private readonly string _maskString;
        private readonly bool _enabled;
        private readonly ILogger<DataSanitizationHelper>? _logger;

        public DataSanitizationHelper(
            List<string>? sensitiveDataPatterns = null,
            string maskString = "***",
            bool enabled = true,
            ILogger<DataSanitizationHelper>? logger = null)
        {
            _enabled = enabled;
            _maskString = maskString;
            _logger = logger;

            // Patrones por defecto si no se proporcionan
            _sensitivePatterns = new List<Regex>();
            var patterns = sensitiveDataPatterns ?? new List<string>
            {
                @"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", // Tarjetas de crédito
                @"\b\d{3}-\d{2}-\d{4}\b", // SSN
                @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b" // Emails
            };

            foreach (var pattern in patterns)
            {
                try
                {
                    _sensitivePatterns.Add(new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Invalid regex pattern for data sanitization: {Pattern}", pattern);
                }
            }
        }

        /// <summary>
        /// Sanitiza un string enmascarando datos sensibles
        /// </summary>
        public string Sanitize(string input)
        {
            if (!_enabled || string.IsNullOrEmpty(input))
                return input;

            var sanitized = input;

            foreach (var pattern in _sensitivePatterns)
            {
                try
                {
                    sanitized = pattern.Replace(sanitized, _maskString);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error applying sanitization pattern");
                }
            }

            return sanitized;
        }

        /// <summary>
        /// Sanitiza un diccionario de propiedades
        /// </summary>
        public Dictionary<string, object?> SanitizeProperties(Dictionary<string, object?> properties)
        {
            if (!_enabled)
                return properties ?? new Dictionary<string, object?>();

            if (properties == null)
                return new Dictionary<string, object?>();

            var sanitized = new Dictionary<string, object?>();

            foreach (var kvp in properties)
            {
                var key = kvp.Key;
                var value = kvp.Value;

                // Sanitizar el valor si es string
                if (value is string stringValue)
                {
                    sanitized[key] = Sanitize(stringValue);
                }
                else
                {
                    sanitized[key] = value;
                }
            }

            return sanitized;
        }

        /// <summary>
        /// Agrega un patrón personalizado para sanitización
        /// </summary>
        public void AddPattern(string pattern)
        {
            try
            {
                _sensitivePatterns.Add(new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Invalid regex pattern: {Pattern}", pattern);
            }
        }
    }
}

