using System.Text;
using System.Text.Json;
using JonjubNet.Observability.Logging.Core;
using JonjubNet.Observability.Logging.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using JonjubNet.Observability.Shared.Utils;

namespace JonjubNet.Observability.Logging.Console
{
    /// <summary>
    /// Sink de logs para Console
    /// Exporta logs desde el Registry a la consola
    /// </summary>
    public class ConsoleLogSink : ILogSink
    {
        private readonly ConsoleOptions _options;
        private readonly ILogger<ConsoleLogSink>? _logger;

        public string Name => "Console";
        public bool IsEnabled => _options.Enabled;

        public ConsoleLogSink(
            IOptions<ConsoleOptions> options,
            ILogger<ConsoleLogSink>? logger = null)
        {
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Exporta logs desde el Registry (método principal optimizado)
        /// </summary>
        public async ValueTask ExportFromRegistryAsync(LogRegistry registry, CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
                return;

            try
            {
                var logs = registry.GetAllLogsAndClear();
                
                if (logs.Count == 0)
                    return;

                foreach (var log in logs)
                {
                    if (ShouldExport(log))
                    {
                        var output = FormatLog(log);
                        System.Console.WriteLine(output);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exporting logs to Console");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Verifica si un log debe ser exportado según el nivel mínimo
        /// </summary>
        private bool ShouldExport(StructuredLogEntry log)
        {
            var minLevel = ParseLogLevel(_options.MinLevel);
            return (int)log.Level >= (int)minLevel;
        }

        /// <summary>
        /// Formatea un log según la configuración
        /// </summary>
        private string FormatLog(StructuredLogEntry log)
        {
            if (_options.Format == "json")
            {
                return FormatAsJson(log);
            }
            else
            {
                return FormatAsText(log);
            }
        }

        /// <summary>
        /// Formatea un log como JSON
        /// </summary>
        private string FormatAsJson(StructuredLogEntry log)
        {
            var jsonOptions = JsonSerializerOptionsCache.GetDefault();
            return JsonSerializer.Serialize(log, jsonOptions);
        }

        /// <summary>
        /// Formatea un log como texto legible
        /// </summary>
        private string FormatAsText(StructuredLogEntry log)
        {
            var sb = new StringBuilder(256);
            
            // Timestamp
            sb.Append(log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            sb.Append(" ");

            // Level con color si está habilitado
            if (_options.UseColors)
            {
                var color = GetLevelColor(log.Level);
                sb.Append($"[{log.Level}]");
            }
            else
            {
                sb.Append($"[{log.Level}]");
            }
            sb.Append(" ");

            // Category
            if (!string.IsNullOrEmpty(log.Category))
            {
                sb.Append($"[{log.Category}] ");
            }

            // Message
            sb.Append(log.Message);

            // Exception
            if (log.Exception != null)
            {
                sb.AppendLine();
                sb.Append($"Exception: {log.Exception.GetType().Name}: {log.Exception.Message}");
                if (log.Exception.StackTrace != null)
                {
                    sb.AppendLine();
                    sb.Append(log.Exception.StackTrace);
                }
            }

            // Properties
            if (log.Properties != null && log.Properties.Count > 0)
            {
                sb.AppendLine();
                sb.Append("Properties: ");
                foreach (var prop in log.Properties)
                {
                    sb.Append($"{prop.Key}={prop.Value}; ");
                }
            }

            // Tags
            if (log.Tags != null && log.Tags.Count > 0)
            {
                sb.AppendLine();
                sb.Append("Tags: ");
                foreach (var tag in log.Tags)
                {
                    sb.Append($"{tag.Key}={tag.Value}; ");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Obtiene el color para un nivel de log (para uso futuro con colores de consola)
        /// </summary>
        private string GetLevelColor(Core.LogLevel level)
        {
            return level switch
            {
                Core.LogLevel.Trace => "Gray",
                Core.LogLevel.Debug => "Cyan",
                Core.LogLevel.Information => "Green",
                Core.LogLevel.Warning => "Yellow",
                Core.LogLevel.Error => "Red",
                Core.LogLevel.Critical => "Magenta",
                _ => "White"
            };
        }

        /// <summary>
        /// Parsea un string a LogLevel
        /// </summary>
        private Core.LogLevel ParseLogLevel(string level)
        {
            return level.ToUpperInvariant() switch
            {
                "TRACE" => Core.LogLevel.Trace,
                "DEBUG" => Core.LogLevel.Debug,
                "INFORMATION" or "INFO" => Core.LogLevel.Information,
                "WARNING" or "WARN" => Core.LogLevel.Warning,
                "ERROR" => Core.LogLevel.Error,
                "CRITICAL" or "FATAL" => Core.LogLevel.Critical,
                _ => Core.LogLevel.Trace
            };
        }
    }
}

