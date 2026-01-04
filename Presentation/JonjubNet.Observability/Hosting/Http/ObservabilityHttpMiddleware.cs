using System.Diagnostics;
using System.Text.RegularExpressions;
using JonjubNet.Observability.Hosting.Configuration;
using JonjubNet.Observability.Logging.Core;
using JonjubNet.Observability.Logging.Core.Interfaces;
using CoreLogLevel = JonjubNet.Observability.Logging.Core.LogLevel;
using JonjubNet.Observability.Metrics.Core.Interfaces;
using JonjubNet.Observability.Shared.Context;
using JonjubNet.Observability.Tracing.Core;
using JonjubNet.Observability.Tracing.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JonjubNet.Observability.Hosting.Http
{
    /// <summary>
    /// Middleware HTTP para correlación automática, logging, métricas y tracing
    /// Optimizado: sin overhead innecesario, thread-safe, sin allocations en hot path
    /// </summary>
    public class ObservabilityHttpMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ObservabilityOptions _options;
        private readonly ILoggingClient? _loggingClient;
        private readonly IMetricsClient? _metricsClient;
        private readonly ITracingClient? _tracingClient;
        private readonly ILogger<ObservabilityHttpMiddleware>? _logger;
        
        // Cache de paths sanitizados (optimización GC)
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _sanitizedPathCache = new();
        
        // Cache de regex compilados (optimización performance)
        private static readonly List<Regex> _pathSanitizationRegexes = new();
        private static readonly object _regexLock = new();

        public ObservabilityHttpMiddleware(
            RequestDelegate next,
            IOptions<ObservabilityOptions> options,
            ILoggingClient? loggingClient = null,
            IMetricsClient? metricsClient = null,
            ITracingClient? tracingClient = null,
            ILogger<ObservabilityHttpMiddleware>? logger = null)
        {
            _next = next;
            _options = options.Value;
            _loggingClient = loggingClient;
            _metricsClient = metricsClient;
            _tracingClient = tracingClient;
            _logger = logger;

            // Compilar regex una vez (optimización)
            if (_pathSanitizationRegexes.Count == 0 && _options.HttpMiddleware.SanitizePathsForMetrics)
            {
                lock (_regexLock)
                {
                    if (_pathSanitizationRegexes.Count == 0)
                    {
                        foreach (var pattern in _options.HttpMiddleware.PathSanitizationPatterns)
                        {
                            _pathSanitizationRegexes.Add(new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant));
                        }
                    }
                }
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Verificar si el middleware está habilitado
            if (!_options.HttpMiddleware.Enabled)
            {
                await _next(context);
                return;
            }

            // Verificar si el path está excluido (optimización: early return)
            if (IsExcludedPath(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Verificar sampling (optimización: early return si no se samplea)
            var shouldSample = ShouldSample();
            if (!shouldSample && !_options.HttpMiddleware.EnableAutomaticMetrics)
            {
                await _next(context);
                return;
            }

            var startTime = Stopwatch.GetTimestamp();
            var startTimeUtc = DateTimeOffset.UtcNow;
            string correlationId;
            ISpan? httpSpan = null;

            try
            {
                // 1. Obtener o generar CorrelationId (identificador único de la transacción)
                // Mejores prácticas: siempre debe haber un CorrelationId disponible
                correlationId = GetOrGenerateCorrelationId(context);

                // 2. Establecer contexto compartido con CorrelationId como identificador principal
                // TraceId y SpanId se generan internamente para correlación de spans (tracing distribuido)
                ObservabilityContext.Set(new ObservabilityContextData
                {
                    CorrelationId = correlationId // Identificador único de la transacción para Logging, Metrics y Tracing
                });

                // 3. Crear span automático si Tracing está habilitado
                if (shouldSample && _options.HttpMiddleware.EnableAutomaticTracing && _tracingClient != null)
                {
                    var operationName = $"{context.Request.Method} {SanitizePath(context.Request.Path)}";
                    httpSpan = _tracingClient.StartSpan(operationName, SpanKind.Server);
                    
                    // Agregar tags HTTP
                    httpSpan.SetTag("http.method", context.Request.Method);
                    httpSpan.SetTag("http.path", context.Request.Path.Value ?? string.Empty);
                    httpSpan.SetTag("http.scheme", context.Request.Scheme);
                    httpSpan.SetTag("http.host", context.Request.Host.Value ?? string.Empty);
                }

                // 4. Logging automático de entrada (si está habilitado)
                if (shouldSample && _options.HttpMiddleware.EnableAutomaticLogging && _loggingClient != null)
                {
                    _loggingClient.LogInformation(
                        $"HTTP Request: {context.Request.Method} {context.Request.Path}",
                        category: "HttpRequest",
                        properties: new Dictionary<string, object?>
                        {
                            ["Method"] = context.Request.Method,
                            ["Path"] = context.Request.Path.Value,
                            ["QueryString"] = context.Request.QueryString.Value,
                            ["UserAgent"] = context.Request.Headers["User-Agent"].ToString(),
                            ["RemoteIpAddress"] = context.Connection.RemoteIpAddress?.ToString()
                        }
                    );
                }

                // 5. Ejecutar siguiente middleware
                await _next(context);

                // 6. Calcular duración
                var durationMs = (Stopwatch.GetTimestamp() - startTime) * 1000.0 / Stopwatch.Frequency;
                var statusCode = context.Response.StatusCode;

                // 7. Finalizar span
                if (httpSpan != null)
                {
                    httpSpan.SetTag("http.status_code", statusCode.ToString());
                    httpSpan.SetTag("http.duration_ms", durationMs.ToString("F2"));
                    
                    if (statusCode >= 400)
                    {
                        httpSpan.Status = SpanStatus.Error;
                        if (statusCode >= 500)
                        {
                            httpSpan.SetTag("error", "true");
                        }
                    }
                    else
                    {
                        httpSpan.Status = SpanStatus.Ok;
                    }
                    
                    httpSpan.Finish();
                }

                // 8. Métricas automáticas (siempre, incluso sin sampling)
                if (_options.HttpMiddleware.EnableAutomaticMetrics && _metricsClient != null)
                {
                    var sanitizedPath = SanitizePath(context.Request.Path);
                    var method = context.Request.Method;
                    var statusCodeStr = statusCode.ToString();

                    // Contador de requests
                    _metricsClient.Increment("http_requests_total", 1.0, new Dictionary<string, string>
                    {
                        ["method"] = method,
                        ["path"] = sanitizedPath,
                        ["status_code"] = statusCodeStr
                    });

                    // Histograma de duración
                    _metricsClient.ObserveHistogram("http_request_duration_seconds", durationMs / 1000.0, new Dictionary<string, string>
                    {
                        ["method"] = method,
                        ["path"] = sanitizedPath,
                        ["status_code"] = statusCodeStr
                    });
                }

                // 9. Logging automático de salida
                if (shouldSample && _options.HttpMiddleware.EnableAutomaticLogging && _loggingClient != null)
                {
                    var logLevel = statusCode >= 500 ? CoreLogLevel.Error : statusCode >= 400 ? CoreLogLevel.Warning : CoreLogLevel.Information;
                    _loggingClient.Log(
                        logLevel,
                        $"HTTP Response: {context.Request.Method} {context.Request.Path} - {statusCode} - {durationMs:F2}ms",
                        category: "HttpRequest"
                    );
                }

                // 10. Agregar header de respuesta con CorrelationId
                // Mejores prácticas: solo CorrelationId como identificador único de transacción
                context.Response.Headers["X-Correlation-Id"] = correlationId;
            }
            catch (Exception ex)
            {
                // Manejo de errores
                var durationMs = (Stopwatch.GetTimestamp() - startTime) * 1000.0 / Stopwatch.Frequency;
                
                if (httpSpan != null)
                {
                    httpSpan.RecordException(ex);
                    httpSpan.Status = SpanStatus.Error;
                    httpSpan.SetTag("error", "true");
                    httpSpan.Finish();
                }

                if (_options.HttpMiddleware.EnableAutomaticLogging && _loggingClient != null)
                {
                    _loggingClient.LogError(
                        $"HTTP Request failed: {context.Request.Method} {context.Request.Path} - {ex.Message}",
                        ex,
                        category: "HttpRequest"
                    );
                }

                if (_options.HttpMiddleware.EnableAutomaticMetrics && _metricsClient != null)
                {
                    var sanitizedPath = SanitizePath(context.Request.Path);
                    _metricsClient.Increment("http_requests_total", 1.0, new Dictionary<string, string>
                    {
                        ["method"] = context.Request.Method,
                        ["path"] = sanitizedPath,
                        ["status_code"] = "500",
                        ["error"] = "true"
                    });
                }

                throw; // Re-lanzar para que el pipeline lo maneje
            }
            finally
            {
                // Limpiar contexto al finalizar request
                ObservabilityContext.Clear();
            }
        }

        /// <summary>
        /// Obtiene CorrelationId del header o genera uno nuevo automáticamente
        /// Mejores prácticas: siempre garantiza que haya un CorrelationId disponible
        /// Optimizado: solo lee header si está configurado
        /// </summary>
        private string GetOrGenerateCorrelationId(HttpContext context)
        {
            // Intentar leer del header si está configurado
            if (_options.Correlation.ReadIncomingCorrelationId)
            {
                var headerValue = context.Request.Headers[_options.Correlation.CorrelationIdHeaderName].FirstOrDefault();
                if (!string.IsNullOrEmpty(headerValue))
                {
                    return headerValue; // Usar el CorrelationId recibido (propagado desde otro microservicio)
                }
            }

            // Si no se recibe, generar uno nuevo automáticamente
            // Esto garantiza que siempre haya trazabilidad, incluso si el request no viene de un orquestador
            return TraceIdGenerator.GenerateCorrelationId();
        }

        /// <summary>
        /// Verifica si el path está excluido
        /// Optimizado: comparación directa sin LINQ
        /// </summary>
        private bool IsExcludedPath(PathString path)
        {
            var pathValue = path.Value ?? string.Empty;
            foreach (var excludedPath in _options.HttpMiddleware.ExcludedPaths)
            {
                if (pathValue.Equals(excludedPath, StringComparison.OrdinalIgnoreCase) ||
                    pathValue.StartsWith(excludedPath + "/", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determina si el request debe ser sampleado
        /// Optimizado: usar Random.Shared (thread-safe, sin allocations)
        /// </summary>
        private bool ShouldSample()
        {
            if (_options.HttpMiddleware.SampleRate >= 1.0)
                return true;
            if (_options.HttpMiddleware.SampleRate <= 0.0)
                return false;

            return Random.Shared.NextDouble() < _options.HttpMiddleware.SampleRate;
        }

        /// <summary>
        /// Sanitiza el path para métricas (reduce cardinalidad)
        /// Optimizado: cache de paths sanitizados, regex compilados
        /// </summary>
        private string SanitizePath(PathString path)
        {
            if (!_options.HttpMiddleware.SanitizePathsForMetrics)
                return path.Value ?? string.Empty;

            var pathValue = path.Value ?? string.Empty;
            
            // Verificar cache primero
            if (_sanitizedPathCache.TryGetValue(pathValue, out var cached))
            {
                return cached;
            }

            // Sanitizar usando regex compilados
            var sanitized = pathValue;
            foreach (var regex in _pathSanitizationRegexes)
            {
                sanitized = regex.Replace(sanitized, "/:id");
            }

            // Cachear resultado (limitar tamaño del cache para evitar memory leaks)
            if (_sanitizedPathCache.Count < 10000)
            {
                _sanitizedPathCache.TryAdd(pathValue, sanitized);
            }

            return sanitized;
        }
    }
}

