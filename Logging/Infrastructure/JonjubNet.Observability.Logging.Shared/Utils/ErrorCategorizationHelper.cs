using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Logging.Shared.Utils
{
    /// <summary>
    /// Helper para categorizar errores automáticamente
    /// Identifica tipos de errores comunes (timeout, connection, validation, etc.)
    /// </summary>
    public class ErrorCategorizationHelper
    {
        private readonly ILogger<ErrorCategorizationHelper>? _logger;
        private readonly Dictionary<string, ErrorCategory> _categoryPatterns;

        public ErrorCategorizationHelper(ILogger<ErrorCategorizationHelper>? logger = null)
        {
            _logger = logger;
            _categoryPatterns = InitializeCategoryPatterns();
        }

        /// <summary>
        /// Categoriza un error basado en su mensaje o tipo
        /// </summary>
        public ErrorCategory Categorize(Exception? exception, string? message = null)
        {
            if (exception == null && string.IsNullOrEmpty(message))
                return ErrorCategory.Unknown;

            var errorText = exception?.ToString() ?? message ?? string.Empty;

            foreach (var pattern in _categoryPatterns)
            {
                if (Regex.IsMatch(errorText, pattern.Key, RegexOptions.IgnoreCase))
                {
                    return pattern.Value;
                }
            }

            // Categorizar por tipo de excepción si no hay match de patrón
            if (exception != null)
            {
                return CategorizeByExceptionType(exception);
            }

            return ErrorCategory.Unknown;
        }

        /// <summary>
        /// Categoriza por tipo de excepción
        /// </summary>
        private ErrorCategory CategorizeByExceptionType(Exception exception)
        {
            return exception switch
            {
                TimeoutException => ErrorCategory.Timeout,
                System.Net.Http.HttpRequestException => ErrorCategory.Network,
                System.IO.IOException => ErrorCategory.IO,
                UnauthorizedAccessException => ErrorCategory.Security,
                ArgumentException or ArgumentNullException => ErrorCategory.Validation,
                InvalidOperationException => ErrorCategory.BusinessLogic,
                NotSupportedException => ErrorCategory.NotSupported,
                OutOfMemoryException => ErrorCategory.Resource,
                StackOverflowException => ErrorCategory.Resource,
                _ => ErrorCategory.Unknown
            };
        }

        /// <summary>
        /// Inicializa los patrones de categorización
        /// </summary>
        private Dictionary<string, ErrorCategory> InitializeCategoryPatterns()
        {
            return new Dictionary<string, ErrorCategory>
            {
                // Timeout patterns
                { @"timeout|timed out|request timeout", ErrorCategory.Timeout },
                { @"connection timeout|read timeout|write timeout", ErrorCategory.Timeout },

                // Network patterns
                { @"connection refused|connection reset|connection closed", ErrorCategory.Network },
                { @"network is unreachable|host unreachable", ErrorCategory.Network },
                { @"dns|name resolution|host not found", ErrorCategory.Network },

                // Validation patterns
                { @"invalid|validation|required|missing|not found", ErrorCategory.Validation },
                { @"format|parse|convert|type mismatch", ErrorCategory.Validation },

                // Security patterns
                { @"unauthorized|forbidden|access denied|authentication|authorization", ErrorCategory.Security },
                { @"token|credential|password|secret", ErrorCategory.Security },

                // Database patterns
                { @"database|sql|query|transaction|constraint", ErrorCategory.Database },
                { @"deadlock|lock timeout|connection pool", ErrorCategory.Database },

                // Resource patterns
                { @"out of memory|insufficient|quota|limit exceeded", ErrorCategory.Resource },
                { @"disk full|no space|file system", ErrorCategory.Resource }
            };
        }
    }

    /// <summary>
    /// Categorías de errores
    /// </summary>
    public enum ErrorCategory
    {
        Unknown,
        Timeout,
        Network,
        Validation,
        Security,
        Database,
        IO,
        BusinessLogic,
        NotSupported,
        Resource
    }
}

