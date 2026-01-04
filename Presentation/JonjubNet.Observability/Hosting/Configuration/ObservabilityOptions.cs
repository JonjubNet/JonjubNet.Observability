namespace JonjubNet.Observability.Hosting.Configuration
{
    /// <summary>
    /// Opciones de configuración para Observability (correlación, middleware, etc.)
    /// </summary>
    public class ObservabilityOptions
    {
        /// <summary>
        /// Configuración de correlación
        /// </summary>
        public CorrelationOptions Correlation { get; set; } = new();

        /// <summary>
        /// Configuración del middleware HTTP
        /// </summary>
        public HttpMiddlewareOptions HttpMiddleware { get; set; } = new();
    }

    /// <summary>
    /// Opciones de correlación
    /// Mejores prácticas: CorrelationId es el identificador único de la transacción
    /// </summary>
    public class CorrelationOptions
    {
        /// <summary>
        /// Si es true, lee X-Correlation-Id del header entrante y lo usa.
        /// Si es false, siempre genera nuevo CorrelationId.
        /// Default: true (para soportar orquestadores que llaman múltiples microservicios)
        /// Mejores prácticas: siempre debe haber un CorrelationId disponible (se genera si no se recibe)
        /// </summary>
        public bool ReadIncomingCorrelationId { get; set; } = true;

        /// <summary>
        /// Nombre del header HTTP para CorrelationId
        /// Default: "X-Correlation-Id"
        /// Este es el identificador único de la transacción que se propaga entre microservicios
        /// </summary>
        public string CorrelationIdHeaderName { get; set; } = "X-Correlation-Id";
    }

    /// <summary>
    /// Opciones del middleware HTTP
    /// </summary>
    public class HttpMiddlewareOptions
    {
        /// <summary>
        /// Habilitar middleware HTTP automático
        /// Default: true
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Habilitar logging automático de requests HTTP
        /// Default: true
        /// </summary>
        public bool EnableAutomaticLogging { get; set; } = true;

        /// <summary>
        /// Habilitar métricas automáticas de requests HTTP
        /// Default: true
        /// </summary>
        public bool EnableAutomaticMetrics { get; set; } = true;

        /// <summary>
        /// Habilitar traces automáticos de requests HTTP
        /// Default: true
        /// </summary>
        public bool EnableAutomaticTracing { get; set; } = true;

        /// <summary>
        /// Sample rate para logging/tracing (0.0 - 1.0)
        /// 1.0 = 100% (todos los requests)
        /// 0.1 = 10% (solo 10% de requests)
        /// Default: 1.0 (todos los requests)
        /// </summary>
        public double SampleRate { get; set; } = 1.0;

        /// <summary>
        /// Paths a excluir del middleware (ej: /health, /metrics)
        /// </summary>
        public List<string> ExcludedPaths { get; set; } = new()
        {
            "/health",
            "/metrics",
            "/favicon.ico"
        };

        /// <summary>
        /// Sanitizar paths dinámicos para métricas (ej: /api/users/123 -> /api/users/:id)
        /// Default: true
        /// </summary>
        public bool SanitizePathsForMetrics { get; set; } = true;

        /// <summary>
        /// Patrones regex para sanitizar paths
        /// </summary>
        public List<string> PathSanitizationPatterns { get; set; } = new()
        {
            @"/\d+",           // Números: /123 -> /:id
            @"/[a-f0-9-]+",   // GUIDs: /550e8400-e29b-41d4-a716-446655440000 -> /:id
            @"/[a-zA-Z0-9]{8,}" // IDs largos: /abc12345 -> /:id
        };
    }
}

