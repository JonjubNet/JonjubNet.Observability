using JonjubNet.Observability.Logging.Core;
using JonjubNet.Observability.Logging.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using SerilogLogger = Serilog.ILogger;

namespace JonjubNet.Observability.Logging.Serilog
{
    /// <summary>
    /// Sink de logs para Serilog
    /// Exporta logs desde el Registry a Serilog
    /// </summary>
    public class SerilogLogSink : ILogSink
    {
        private readonly SerilogOptions _options;
        private readonly ILogger<SerilogLogSink>? _logger;
        private readonly SerilogLogger? _serilogLogger;

        public string Name => "Serilog";
        public bool IsEnabled => _options.Enabled;

        public SerilogLogSink(
            IOptions<SerilogOptions> options,
            ILogger<SerilogLogSink>? logger = null,
            SerilogLogger? serilogLogger = null)
        {
            _options = options.Value;
            _logger = logger;
            _serilogLogger = serilogLogger ?? Log.Logger;
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
                    WriteToSerilog(log);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exporting logs to Serilog");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Escribe un log a Serilog
        /// </summary>
        private void WriteToSerilog(StructuredLogEntry log)
        {
            var level = ConvertToSerilogLevel(log.Level);
            var messageTemplate = log.Message;

            // Crear propiedades para Serilog
            var properties = new List<LogEventProperty>();
            
            // Agregar propiedades estándar
            if (!string.IsNullOrEmpty(log.Category))
            {
                properties.Add(new LogEventProperty("Category", new ScalarValue(log.Category)));
            }

            if (log.Exception != null)
            {
                properties.Add(new LogEventProperty("Exception", new ScalarValue(log.Exception)));
            }

            // Agregar propiedades personalizadas
            if (log.Properties != null)
            {
                foreach (var prop in log.Properties)
                {
                    properties.Add(new LogEventProperty(prop.Key, new ScalarValue(prop.Value?.ToString() ?? "")));
                }
            }

            // Agregar tags
            if (log.Tags != null)
            {
                foreach (var tag in log.Tags)
                {
                    properties.Add(new LogEventProperty($"Tag_{tag.Key}", new ScalarValue(tag.Value)));
                }
            }

            // Agregar contexto de correlación
            if (!string.IsNullOrEmpty(log.CorrelationId))
            {
                properties.Add(new LogEventProperty("CorrelationId", new ScalarValue(log.CorrelationId)));
            }

            if (!string.IsNullOrEmpty(log.RequestId))
            {
                properties.Add(new LogEventProperty("RequestId", new ScalarValue(log.RequestId)));
            }

            if (!string.IsNullOrEmpty(log.SessionId))
            {
                properties.Add(new LogEventProperty("SessionId", new ScalarValue(log.SessionId)));
            }

            if (!string.IsNullOrEmpty(log.UserId))
            {
                properties.Add(new LogEventProperty("UserId", new ScalarValue(log.UserId)));
            }

            if (!string.IsNullOrEmpty(log.Operation))
            {
                properties.Add(new LogEventProperty("Operation", new ScalarValue(log.Operation)));
            }

            if (log.DurationMs.HasValue)
            {
                properties.Add(new LogEventProperty("DurationMs", new ScalarValue(log.DurationMs.Value)));
            }

            // Escribir a Serilog usando el método Write con propiedades
            // Crear un logger enriquecido con las propiedades
            var enrichedLogger = _serilogLogger;
            foreach (var prop in properties)
            {
                enrichedLogger = enrichedLogger?.ForContext(prop.Name, prop.Value);
            }
            
            // Escribir el log
            enrichedLogger?.Write(level, messageTemplate);
        }

        /// <summary>
        /// Convierte LogLevel a Serilog LogEventLevel
        /// </summary>
        private LogEventLevel ConvertToSerilogLevel(Core.LogLevel level)
        {
            return level switch
            {
                Core.LogLevel.Trace => LogEventLevel.Verbose,
                Core.LogLevel.Debug => LogEventLevel.Debug,
                Core.LogLevel.Information => LogEventLevel.Information,
                Core.LogLevel.Warning => LogEventLevel.Warning,
                Core.LogLevel.Error => LogEventLevel.Error,
                Core.LogLevel.Critical => LogEventLevel.Fatal,
                _ => LogEventLevel.Information
            };
        }
    }
}

