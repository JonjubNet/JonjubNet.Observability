using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Metrics.Shared.Security
{
    /// <summary>
    /// Validador de etiquetas para prevenir inyección de métricas y PII
    /// </summary>
    public class SecureTagValidator
    {
        private readonly ILogger<SecureTagValidator>? _logger;
        private readonly HashSet<string> _blacklistedKeys;
        private readonly List<Regex> _sensitivePatterns;

        public SecureTagValidator(ILogger<SecureTagValidator>? logger = null)
        {
            _logger = logger;
            _blacklistedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "password", "passwd", "pwd", "secret", "token", "api_key", "apikey",
                "auth", "authorization", "credit_card", "cc_number", "ssn", "social_security",
                "email", "phone", "phone_number", "creditcard", "cvv"
            };

            _sensitivePatterns = new List<Regex>
            {
                new(@"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b"), // Credit card
                new(@"\b\d{3}-\d{2}-\d{4}\b"), // SSN
                new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b"), // Email
                new(@"\b\d{3}-\d{3}-\d{4}\b") // Phone
            };
        }

        /// <summary>
        /// Valida y sanitiza etiquetas
        /// </summary>
        public Dictionary<string, string> ValidateAndSanitize(Dictionary<string, string>? tags)
        {
            if (tags == null)
                return new Dictionary<string, string>();

            var sanitized = new Dictionary<string, string>();

            foreach (var kvp in tags)
            {
                var key = SanitizeKey(kvp.Key);
                var value = SanitizeValue(kvp.Value);

                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                {
                    sanitized[key] = value;
                }
            }

            return sanitized;
        }

        /// <summary>
        /// Valida si una clave es segura
        /// </summary>
        public bool IsKeySafe(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            if (_blacklistedKeys.Contains(key))
            {
                _logger?.LogWarning("Blacklisted tag key detected: {Key}", key);
                return false;
            }

            // Validar formato (solo letras, números, guiones bajos)
            if (!Regex.IsMatch(key, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                _logger?.LogWarning("Invalid tag key format: {Key}", key);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Valida si un valor es seguro
        /// </summary>
        public bool IsValueSafe(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            foreach (var pattern in _sensitivePatterns)
            {
                if (pattern.IsMatch(value))
                {
                    _logger?.LogWarning("Sensitive data pattern detected in tag value");
                    return false;
                }
            }

            return true;
        }

        private string SanitizeKey(string key)
        {
            if (!IsKeySafe(key))
                return string.Empty;

            // Normalizar a snake_case
            return Regex.Replace(key, @"[^a-zA-Z0-9_]", "_");
        }

        private string SanitizeValue(string value)
        {
            if (!IsValueSafe(value))
            {
                _logger?.LogWarning("Tag value contains sensitive data, replacing with [REDACTED]");
                return "[REDACTED]";
            }

            // Limitar longitud
            const int maxLength = 256;
            if (value.Length > maxLength)
            {
                return value.Substring(0, maxLength);
            }

            return value;
        }
    }
}

