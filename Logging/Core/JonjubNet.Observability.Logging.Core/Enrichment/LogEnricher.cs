using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Logging.Core.Enrichment
{
    /// <summary>
    /// Enricher para logs
    /// Enriquece logs con información adicional (usuario, HTTP context, ambiente, versión, etc.)
    /// </summary>
    public class LogEnricher
    {
        private readonly ILogger<LogEnricher>? _logger;
        private readonly EnrichmentOptions _options;

        public LogEnricher(
            EnrichmentOptions? options = null,
            ILogger<LogEnricher>? logger = null)
        {
            _options = options ?? new EnrichmentOptions();
            _logger = logger;
        }

        /// <summary>
        /// Enriquece un log con información adicional
        /// </summary>
        public void Enrich(StructuredLogEntry log)
        {
            // Enriquecer con información del ambiente
            if (_options.IncludeEnvironment && !string.IsNullOrEmpty(_options.Environment))
            {
                log.Properties.TryAdd("Environment", _options.Environment);
            }

            // Enriquecer con información de la versión
            if (_options.IncludeVersion && !string.IsNullOrEmpty(_options.Version))
            {
                log.Properties.TryAdd("Version", _options.Version);
            }

            // Enriquecer con información del servicio
            if (_options.IncludeServiceName && !string.IsNullOrEmpty(_options.ServiceName))
            {
                log.Properties.TryAdd("ServiceName", _options.ServiceName);
            }

            // Enriquecer con información de la máquina
            if (_options.IncludeMachineName)
            {
                log.Properties.TryAdd("MachineName", Environment.MachineName);
            }

            // Enriquecer con información del proceso
            if (_options.IncludeProcessInfo)
            {
                var process = Process.GetCurrentProcess();
                log.Properties.TryAdd("ProcessId", process.Id);
                log.Properties.TryAdd("ProcessName", process.ProcessName);
            }

            // Enriquecer con información de threads
            if (_options.IncludeThreadInfo)
            {
                log.Properties.TryAdd("ThreadId", Thread.CurrentThread.ManagedThreadId);
                log.Properties.TryAdd("ThreadName", Thread.CurrentThread.Name ?? "Unknown");
            }

            // Enriquecer con información de usuario (si está disponible)
            if (_options.IncludeUserInfo && !string.IsNullOrEmpty(_options.UserId))
            {
                log.UserId = _options.UserId;
            }

            // Enriquecer con información de correlación (si está disponible)
            if (_options.IncludeCorrelationId && !string.IsNullOrEmpty(_options.CorrelationId))
            {
                log.CorrelationId = _options.CorrelationId;
            }

            // Enriquecer con información de request (si está disponible)
            if (_options.IncludeRequestId && !string.IsNullOrEmpty(_options.RequestId))
            {
                log.RequestId = _options.RequestId;
            }

            // Enriquecer con información de sesión (si está disponible)
            if (_options.IncludeSessionId && !string.IsNullOrEmpty(_options.SessionId))
            {
                log.SessionId = _options.SessionId;
            }

            // Agregar propiedades personalizadas
            if (_options.CustomProperties != null && _options.CustomProperties.Count > 0)
            {
                foreach (var prop in _options.CustomProperties)
                {
                    log.Properties.TryAdd(prop.Key, prop.Value);
                }
            }

            // Agregar tags personalizados
            if (_options.CustomTags != null && _options.CustomTags.Count > 0)
            {
                foreach (var tag in _options.CustomTags)
                {
                    log.Tags.TryAdd(tag.Key, tag.Value);
                }
            }
        }
    }

    /// <summary>
    /// Opciones de enriquecimiento
    /// </summary>
    public class EnrichmentOptions
    {
        /// <summary>
        /// Incluir información del ambiente
        /// </summary>
        public bool IncludeEnvironment { get; set; } = true;

        /// <summary>
        /// Ambiente actual
        /// </summary>
        public string? Environment { get; set; }

        /// <summary>
        /// Incluir información de la versión
        /// </summary>
        public bool IncludeVersion { get; set; } = true;

        /// <summary>
        /// Versión actual
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Incluir nombre del servicio
        /// </summary>
        public bool IncludeServiceName { get; set; } = true;

        /// <summary>
        /// Nombre del servicio
        /// </summary>
        public string? ServiceName { get; set; }

        /// <summary>
        /// Incluir nombre de la máquina
        /// </summary>
        public bool IncludeMachineName { get; set; } = true;

        /// <summary>
        /// Incluir información del proceso
        /// </summary>
        public bool IncludeProcessInfo { get; set; } = false;

        /// <summary>
        /// Incluir información de threads
        /// </summary>
        public bool IncludeThreadInfo { get; set; } = false;

        /// <summary>
        /// Incluir información de usuario
        /// </summary>
        public bool IncludeUserInfo { get; set; } = true;

        /// <summary>
        /// ID del usuario actual
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Incluir CorrelationId
        /// </summary>
        public bool IncludeCorrelationId { get; set; } = true;

        /// <summary>
        /// CorrelationId actual
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Incluir RequestId
        /// </summary>
        public bool IncludeRequestId { get; set; } = true;

        /// <summary>
        /// RequestId actual
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// Incluir SessionId
        /// </summary>
        public bool IncludeSessionId { get; set; } = true;

        /// <summary>
        /// SessionId actual
        /// </summary>
        public string? SessionId { get; set; }

        /// <summary>
        /// Propiedades personalizadas adicionales
        /// </summary>
        public Dictionary<string, object?>? CustomProperties { get; set; }

        /// <summary>
        /// Tags personalizados adicionales
        /// </summary>
        public Dictionary<string, string>? CustomTags { get; set; }
    }
}

