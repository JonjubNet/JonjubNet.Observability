using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Logging.Core.Filters
{
    /// <summary>
    /// Filtro para logs
    /// Permite filtrar logs por nivel, categoría, operación, usuario, etc.
    /// </summary>
    public class LogFilter
    {
        private readonly ILogger<LogFilter>? _logger;
        private readonly FilterOptions _options;

        public LogFilter(
            FilterOptions? options = null,
            ILogger<LogFilter>? logger = null)
        {
            _options = options ?? new FilterOptions();
            _logger = logger;
        }

        /// <summary>
        /// Determina si un log debe ser procesado según los filtros configurados
        /// </summary>
        public bool ShouldProcess(StructuredLogEntry log)
        {
            // Filtrar por nivel mínimo
            if (_options.MinLevel.HasValue)
            {
                if ((int)log.Level < (int)_options.MinLevel.Value)
                {
                    return false;
                }
            }

            // Filtrar por nivel máximo
            if (_options.MaxLevel.HasValue)
            {
                if ((int)log.Level > (int)_options.MaxLevel.Value)
                {
                    return false;
                }
            }

            // Filtrar por categoría
            if (_options.ExcludedCategories != null && _options.ExcludedCategories.Count > 0)
            {
                if (_options.ExcludedCategories.Contains(log.Category, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Filtrar por categorías permitidas (si está configurado)
            if (_options.AllowedCategories != null && _options.AllowedCategories.Count > 0)
            {
                if (!_options.AllowedCategories.Contains(log.Category, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Filtrar por operación
            if (!string.IsNullOrEmpty(_options.ExcludedOperation) && log.Operation == _options.ExcludedOperation)
            {
                return false;
            }

            // Filtrar por usuario
            if (!string.IsNullOrEmpty(_options.ExcludedUserId) && log.UserId == _options.ExcludedUserId)
            {
                return false;
            }

            // Filtrar por tags
            if (_options.ExcludedTags != null && _options.ExcludedTags.Count > 0)
            {
                foreach (var excludedTag in _options.ExcludedTags)
                {
                    if (log.Tags.ContainsKey(excludedTag.Key) && log.Tags[excludedTag.Key] == excludedTag.Value)
                    {
                        return false;
                    }
                }
            }

            // Filtrar por mensaje (patrones regex)
            if (_options.ExcludedMessagePatterns != null && _options.ExcludedMessagePatterns.Count > 0)
            {
                foreach (var pattern in _options.ExcludedMessagePatterns)
                {
                    try
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(log.Message, pattern))
                        {
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Invalid regex pattern in filter: {Pattern}", pattern);
                    }
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Opciones de filtrado
    /// </summary>
    public class FilterOptions
    {
        /// <summary>
        /// Nivel mínimo de log a procesar
        /// </summary>
        public LogLevel? MinLevel { get; set; }

        /// <summary>
        /// Nivel máximo de log a procesar
        /// </summary>
        public LogLevel? MaxLevel { get; set; }

        /// <summary>
        /// Categorías excluidas
        /// </summary>
        public List<string>? ExcludedCategories { get; set; }

        /// <summary>
        /// Categorías permitidas (si está configurado, solo estas se procesan)
        /// </summary>
        public List<string>? AllowedCategories { get; set; }

        /// <summary>
        /// Operación excluida
        /// </summary>
        public string? ExcludedOperation { get; set; }

        /// <summary>
        /// Usuario excluido
        /// </summary>
        public string? ExcludedUserId { get; set; }

        /// <summary>
        /// Tags excluidos (key-value pairs)
        /// </summary>
        public Dictionary<string, string>? ExcludedTags { get; set; }

        /// <summary>
        /// Patrones de mensaje excluidos (regex)
        /// </summary>
        public List<string>? ExcludedMessagePatterns { get; set; }
    }
}

