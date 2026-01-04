using System.Security.Cryptography.X509Certificates;
using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.Core.Interfaces;
using JonjubNet.Observability.Metrics.Core.Resilience;
using JonjubNet.Observability.Metrics.Prometheus;
using JonjubNet.Observability.Metrics.Shared.Configuration;
using JonjubNet.Observability.Metrics.Shared.Health;
using JonjubNet.Observability.Metrics.Shared.Resilience;
using JonjubNet.Observability.Shared.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JonjubNet.Observability.Hosting
{
    /// <summary>
    /// Extensiones para registro de servicios de métricas
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Agrega la infraestructura de métricas
        /// </summary>
        public static IServiceCollection AddJonjubNetMetrics(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<MetricsOptions>? configureOptions = null)
        {
            // Configurar opciones nuevas
            services.Configure<MetricsOptions>(options =>
            {
                configuration.GetSection("Metrics").Bind(options);
                configureOptions?.Invoke(options);
            });

            // Configurar MetricsConfiguration para compatibilidad con código existente
            services.Configure<MetricsConfiguration>(configuration.GetSection(MetricsConfiguration.SectionName));

            // Registrar servicios de encriptación
            services.AddSingleton<EncryptionService>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MetricsOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<EncryptionService>>();
                
                // Si hay claves configuradas, usarlas; sino generar automáticamente
                if (!string.IsNullOrEmpty(options.Encryption.EncryptionKeyBase64) && 
                    !string.IsNullOrEmpty(options.Encryption.EncryptionIVBase64))
                {
                    var key = Convert.FromBase64String(options.Encryption.EncryptionKeyBase64);
                    var iv = Convert.FromBase64String(options.Encryption.EncryptionIVBase64);
                    return new EncryptionService(key, iv, logger);
                }
                
                return new EncryptionService(logger);
            });

            // Registrar SecureHttpClientFactory
            services.AddSingleton<SecureHttpClientFactory>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MetricsOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<SecureHttpClientFactory>>();
                
                X509Certificate2? clientCert = null;
                if (!string.IsNullOrEmpty(options.Encryption.EncryptionKeyBase64))
                {
                    // En producción, cargar certificado desde configuración segura
                    // Por ahora, null es aceptable
                }
                
                return new SecureHttpClientFactory(
                    validateCertificates: options.Encryption.ValidateCertificates,
                    clientCertificate: clientCert,
                    logger: logger);
            });

            // Registrar componentes Core
            services.AddSingleton<MetricRegistry>();
            // ELIMINADO: MetricBus ya no se necesita - todos los sinks leen del Registry
            
            // Registrar MetricsClient (simplificado - solo Registry)
            services.AddSingleton<IMetricsClient>(sp =>
            {
                var registry = sp.GetRequiredService<MetricRegistry>();
                return new MetricsClient(registry);
            });

            // Registrar Dead Letter Queue si está habilitada
            services.AddSingleton<DeadLetterQueue>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MetricsOptions>>().Value;
                if (!options.DeadLetterQueue.Enabled)
                {
                    return null!; // Retornar null si está deshabilitada
                }
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<DeadLetterQueue>>();
                var encryptionService = options.Encryption.EnableAtRest 
                    ? sp.GetService<EncryptionService>() 
                    : null;
                return new DeadLetterQueue(
                    options.DeadLetterQueue.MaxSize, 
                    logger,
                    encryptionService,
                    options.Encryption.EnableAtRest);
            });

            // Registrar Retry Policy si está habilitada
            services.AddSingleton<RetryPolicy>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MetricsOptions>>().Value;
                if (!options.RetryPolicy.Enabled)
                {
                    return null!; // Retornar null si está deshabilitada
                }
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<RetryPolicy>>();
                return new RetryPolicy(
                    options.RetryPolicy.MaxRetries,
                    TimeSpan.FromMilliseconds(options.RetryPolicy.InitialDelayMs),
                    options.RetryPolicy.BackoffMultiplier,
                    options.RetryPolicy.JitterPercent,
                    logger);
            });

            // Registrar SinkCircuitBreakerManager si está habilitado
            services.AddSingleton<ISinkCircuitBreakerManager>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MetricsOptions>>().Value;
                if (!options.CircuitBreaker.Enabled)
                {
                    return null!; // Retornar null si está deshabilitado
                }
                
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<SinkCircuitBreakerManager>>();
                
                // Crear opciones por defecto
                var defaultOptions = new CircuitBreakerOptions
                {
                    Enabled = true,
                    FailureThreshold = options.CircuitBreaker.Default.FailureThreshold,
                    OpenDuration = TimeSpan.FromSeconds(options.CircuitBreaker.Default.OpenDurationSeconds)
                };
                
                // Crear opciones específicas por sink
                var sinkSpecificOptions = new Dictionary<string, CircuitBreakerOptions>();
                foreach (var kvp in options.CircuitBreaker.Sinks)
                {
                    var sinkOptions = new CircuitBreakerOptions
                    {
                        Enabled = kvp.Value.Enabled,
                        FailureThreshold = kvp.Value.FailureThreshold ?? defaultOptions.FailureThreshold,
                        OpenDuration = TimeSpan.FromSeconds(kvp.Value.OpenDurationSeconds ?? options.CircuitBreaker.Default.OpenDurationSeconds)
                    };
                    sinkSpecificOptions[kvp.Key] = sinkOptions;
                }
                
                return new SinkCircuitBreakerManager(
                    defaultOptions,
                    sinkSpecificOptions,
                    logger,
                    enabled: options.CircuitBreaker.Enabled);
            });

            // Registrar scheduler simplificado (lee del Registry, sin Bus)
            services.AddSingleton<MetricFlushScheduler>(sp =>
            {
                var registry = sp.GetRequiredService<MetricRegistry>();
                var sinks = sp.GetServices<IMetricsSink>();
                var options = sp.GetRequiredService<IOptions<MetricsOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<MetricFlushScheduler>>();
                var deadLetterQueue = options.DeadLetterQueue.Enabled 
                    ? sp.GetService<DeadLetterQueue>() 
                    : null;
                var retryPolicy = options.RetryPolicy.Enabled 
                    ? sp.GetService<RetryPolicy>() 
                    : null;
                var circuitBreakerManager = options.CircuitBreaker.Enabled
                    ? sp.GetService<ISinkCircuitBreakerManager>()
                    : null;
                
                return new MetricFlushScheduler(
                    registry,
                    sinks,
                    TimeSpan.FromMilliseconds(options.FlushIntervalMs),
                    logger,
                    deadLetterQueue,
                    retryPolicy,
                    circuitBreakerManager);
            });

            // Registrar procesador de DLQ si está habilitado
            services.AddHostedService<DeadLetterQueueProcessor>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MetricsOptions>>().Value;
                if (!options.DeadLetterQueue.Enabled || !options.DeadLetterQueue.EnableAutoProcessing)
                {
                    return null!; // No registrar si está deshabilitado
                }
                var deadLetterQueue = sp.GetRequiredService<DeadLetterQueue>();
                var sinks = sp.GetServices<IMetricsSink>();
                var retryPolicy = options.RetryPolicy.Enabled 
                    ? sp.GetService<RetryPolicy>() 
                    : null;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<DeadLetterQueueProcessor>>();
                return new DeadLetterQueueProcessor(
                    deadLetterQueue,
                    sinks,
                    retryPolicy,
                    TimeSpan.FromMilliseconds(options.DeadLetterQueue.ProcessingIntervalMs),
                    options.BatchSize,
                    logger);
            });

            // Registrar background service
            services.AddHostedService<MetricsBackgroundService>();
            
            // Registrar MetricsConfigWatcher para monitorear cambios de configuración (Paso 7.5)
            services.AddHostedService<MetricsConfigWatcher>();

            // Registrar seguridad
            services.AddSingleton<Metrics.Shared.Security.SecureTagValidator>();

            // Registrar configuración
            // Nota sobre logging: El componente utiliza ILogger estándar de Microsoft.Extensions.Logging
            // para todos los eventos (errores, warnings, información, debug). Si tu proyecto utiliza
            // Jonjub.Logging, puedes configurarlo como proveedor de logging y todos los eventos
            // del componente de métricas se registrarán a través de él automáticamente.
            services.AddSingleton<MetricsConfigurationManager>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<MetricsConfigurationManager>>();
                return new MetricsConfigurationManager(configuration, logger);
            });

            // Configurar Prometheus por defecto
            services.Configure<PrometheusOptions>(options =>
            {
                configuration.GetSection("Metrics:Prometheus").Bind(options);
            });
            services.AddSingleton<PrometheusFormatter>();
            services.AddSingleton<PrometheusExporter>();
            services.AddSingleton<IMetricsSink>(sp => sp.GetRequiredService<PrometheusExporter>());

            // Registrar sinks con encriptación si están configurados
            RegisterSinksWithEncryption(services, configuration);

            // Registrar health check (usando el de Shared.Health) - sin Bus
            services.AddSingleton<JonjubNet.Observability.Metrics.Shared.Health.IMetricsHealthCheck>(sp =>
            {
                // ELIMINADO: Bus ya no se necesita
                var sinks = sp.GetServices<IMetricsSink>();
                var scheduler = sp.GetService<MetricFlushScheduler>();
                var options = sp.GetRequiredService<IOptions<MetricsOptions>>();
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<JonjubNet.Observability.Metrics.Shared.Health.MetricsHealthCheck>>();
                return new JonjubNet.Observability.Metrics.Shared.Health.MetricsHealthCheck(sinks, scheduler, options, logger);
            });

            // Registrar health check para ASP.NET Core
            services.AddHealthChecks()
                .AddCheck<Health.MetricsHealthCheckService>("metrics");

            return services;
        }

        /// <summary>
        /// Agrega la infraestructura de logging
        /// </summary>
        public static IServiceCollection AddJonjubNetLogging(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<Logging.Shared.Configuration.LoggingOptions>? configureOptions = null)
        {

            // Configurar opciones
            services.Configure<Logging.Shared.Configuration.LoggingOptions>(options =>
            {
                configuration.GetSection("JonjubNet:Logging").Bind(options);
                configuration.GetSection("Logging").Bind(options);
                configureOptions?.Invoke(options);
            });

            // Registrar servicios de encriptación (reutilizar de Shared)
            services.AddSingleton<EncryptionService>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Logging.Shared.Configuration.LoggingOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<EncryptionService>>();
                
                if (!string.IsNullOrEmpty(options.Encryption.EncryptionKey))
                {
                    // Si hay clave configurada, usarla
                    var key = Convert.FromBase64String(options.Encryption.EncryptionKey);
                    return new EncryptionService(key, Array.Empty<byte>(), logger);
                }
                
                return new EncryptionService(logger);
            });

            // Registrar SecureHttpClientFactory
            services.AddSingleton<SecureHttpClientFactory>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Logging.Shared.Configuration.LoggingOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<SecureHttpClientFactory>>();
                
                return new SecureHttpClientFactory(
                    validateCertificates: true,
                    clientCertificate: null,
                    logger: logger);
            });

            // Registrar KafkaProducerFactory
            services.AddSingleton<Shared.Kafka.KafkaProducerFactory>();

            // Registrar componentes Core
            services.AddSingleton<Logging.Core.LogRegistry>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Logging.Shared.Configuration.LoggingOptions>>().Value;
                var registry = new Logging.Core.LogRegistry();
                registry.MaxSize = options.RegistryCapacity;
                return registry;
            });

            services.AddSingleton<Logging.Core.LogScopeManager>();

            // Registrar LogFilter
            services.AddSingleton<Logging.Core.Filters.LogFilter>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Logging.Shared.Configuration.LoggingOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Logging.Core.Filters.LogFilter>>();
                
                var filterOptions = new Logging.Core.Filters.FilterOptions
                {
                    MinLevel = ParseLogLevel(options.MinLevel),
                    MaxLevel = null
                };
                
                return new Logging.Core.Filters.LogFilter(filterOptions, logger);
            });

            // Registrar LogEnricher
            services.AddSingleton<Logging.Core.Enrichment.LogEnricher>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Logging.Shared.Configuration.LoggingOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Logging.Core.Enrichment.LogEnricher>>();
                
                var enrichmentOptions = new Logging.Core.Enrichment.EnrichmentOptions
                {
                    Environment = options.Environment,
                    Version = options.Version,
                    ServiceName = options.ServiceName,
                    IncludeEnvironment = true,
                    IncludeVersion = true,
                    IncludeServiceName = true,
                    IncludeMachineName = true
                };
                
                return new Logging.Core.Enrichment.LogEnricher(enrichmentOptions, logger);
            });

            // Registrar LoggingClient
            services.AddSingleton<Logging.Core.Interfaces.ILoggingClient>(sp =>
            {
                var registry = sp.GetRequiredService<Logging.Core.LogRegistry>();
                var scopeManager = sp.GetRequiredService<Logging.Core.LogScopeManager>();
                var filter = sp.GetService<Logging.Core.Filters.LogFilter>();
                var enricher = sp.GetService<Logging.Core.Enrichment.LogEnricher>();
                return new Logging.Core.LoggingClient(registry, scopeManager, filter, enricher);
            });

            // Registrar Dead Letter Queue si está habilitada
            services.AddSingleton<Logging.Core.Resilience.DeadLetterQueue>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Logging.Shared.Configuration.LoggingOptions>>().Value;
                if (!options.DeadLetterQueue.Enabled)
                {
                    return null!;
                }
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Logging.Core.Resilience.DeadLetterQueue>>();
                var encryptionService = options.DeadLetterQueue.EncryptAtRest 
                    ? sp.GetService<EncryptionService>() 
                    : null;
                return new Logging.Core.Resilience.DeadLetterQueue(
                    maxSize: options.DeadLetterQueue.MaxSize,
                    logger: logger,
                    encryptionService: encryptionService,
                    encryptAtRest: options.DeadLetterQueue.EncryptAtRest);
            });

            // Registrar RetryPolicy si está habilitada
            services.AddSingleton<Logging.Core.Resilience.RetryPolicy>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Logging.Shared.Configuration.LoggingOptions>>().Value;
                if (!options.RetryPolicy.Enabled)
                {
                    return null!;
                }
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Logging.Core.Resilience.RetryPolicy>>();
                return new Logging.Core.Resilience.RetryPolicy(
                    maxRetries: options.RetryPolicy.MaxRetries,
                    initialDelay: TimeSpan.FromMilliseconds(options.RetryPolicy.InitialDelayMs),
                    backoffMultiplier: options.RetryPolicy.BackoffMultiplier,
                    jitterPercent: options.RetryPolicy.JitterPercent,
                    logger: logger);
            });

            // Registrar SinkCircuitBreakerManager
            services.AddSingleton<Logging.Shared.Resilience.SinkCircuitBreakerManager>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Logging.Shared.Configuration.LoggingOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Logging.Shared.Resilience.SinkCircuitBreakerManager>>();
                
                var defaultOptions = new Logging.Shared.Resilience.CircuitBreakerOptions
                {
                    FailureThreshold = options.CircuitBreaker.Default.FailureThreshold,
                    OpenDurationSeconds = options.CircuitBreaker.Default.OpenDurationSeconds
                };
                
                var sinkOptions = new Dictionary<string, Logging.Shared.Resilience.CircuitBreakerOptions>();
                foreach (var kvp in options.CircuitBreaker.Sinks)
                {
                    sinkOptions[kvp.Key] = new Logging.Shared.Resilience.CircuitBreakerOptions
                    {
                        FailureThreshold = kvp.Value.FailureThreshold,
                        OpenDurationSeconds = kvp.Value.OpenDurationSeconds
                    };
                }
                
                return new Logging.Shared.Resilience.SinkCircuitBreakerManager(
                    defaultOptions: defaultOptions,
                    sinkSpecificOptions: sinkOptions,
                    logger: logger,
                    enabled: options.CircuitBreaker.Enabled);
            });

            // Registrar LogFlushScheduler
            services.AddSingleton<Logging.Core.LogFlushScheduler>(sp =>
            {
                var registry = sp.GetRequiredService<Logging.Core.LogRegistry>();
                var sinks = sp.GetServices<Logging.Core.Interfaces.ILogSink>();
                var options = sp.GetRequiredService<IOptions<Logging.Shared.Configuration.LoggingOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Logging.Core.LogFlushScheduler>>();
                var deadLetterQueue = sp.GetService<Logging.Core.Resilience.DeadLetterQueue>();
                var retryPolicy = sp.GetService<Logging.Core.Resilience.RetryPolicy>();
                var circuitBreakerManager = sp.GetService<Logging.Shared.Resilience.SinkCircuitBreakerManager>();
                
                return new Logging.Core.LogFlushScheduler(
                    registry: registry,
                    sinks: sinks,
                    exportInterval: TimeSpan.FromMilliseconds(options.FlushIntervalMs),
                    logger: logger,
                    deadLetterQueue: deadLetterQueue,
                    retryPolicy: retryPolicy,
                    circuitBreakerManager: circuitBreakerManager);
            });

            // Registrar helpers
            services.AddSingleton<Logging.Shared.Utils.LogSamplingHelper>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Logging.Shared.Configuration.LoggingOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Logging.Shared.Utils.LogSamplingHelper>>();
                return new Logging.Shared.Utils.LogSamplingHelper(
                    probability: options.Sampling.Probability,
                    rateLimitPerSecond: options.Sampling.RateLimitPerSecond,
                    enabled: options.Sampling.Enabled,
                    logger: logger);
            });

            services.AddSingleton<Logging.Shared.Utils.DataSanitizationHelper>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Logging.Shared.Configuration.LoggingOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Logging.Shared.Utils.DataSanitizationHelper>>();
                return new Logging.Shared.Utils.DataSanitizationHelper(
                    sensitiveDataPatterns: options.DataSanitization.SensitiveDataPatterns,
                    maskString: options.DataSanitization.MaskString,
                    enabled: options.DataSanitization.Enabled,
                    logger: logger);
            });

            services.AddSingleton<Logging.Shared.Utils.ErrorCategorizationHelper>();
            services.AddSingleton<Logging.Shared.Utils.BatchingHelper<Logging.Core.StructuredLogEntry>>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Logging.Shared.Configuration.LoggingOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Logging.Shared.Utils.BatchingHelper<Logging.Core.StructuredLogEntry>>>();
                return new Logging.Shared.Utils.BatchingHelper<Logging.Core.StructuredLogEntry>(
                    maxBatchSize: options.BatchSize,
                    maxBatchAge: TimeSpan.FromMilliseconds(options.FlushIntervalMs),
                    logger: logger);
            });

            // Registrar sinks
            RegisterLoggingSinks(services, configuration);

            // Registrar background service
            services.AddHostedService<LoggingBackgroundService>();
            
            // Registrar LoggingConfigWatcher
            services.AddHostedService<LoggingConfigWatcher>();

            // Registrar configuración
            services.AddSingleton<Logging.Shared.Configuration.LoggingConfigurationManager>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Logging.Shared.Configuration.LoggingConfigurationManager>>();
                return new Logging.Shared.Configuration.LoggingConfigurationManager(config, logger);
            });

            // Registrar health check
            services.AddSingleton<Logging.Shared.Health.LoggingHealthCheck>(sp =>
            {
                var registry = sp.GetRequiredService<Logging.Core.LogRegistry>();
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Logging.Shared.Health.LoggingHealthCheck>>();
                return new Logging.Shared.Health.LoggingHealthCheck(registry, logger);
            });

            services.AddHealthChecks()
                .AddCheck<Health.LoggingHealthCheckService>("logging");

            return services;
        }

        private static Logging.Core.LogLevel ParseLogLevel(string level)
        {
            return level.ToUpperInvariant() switch
            {
                "TRACE" => Logging.Core.LogLevel.Trace,
                "DEBUG" => Logging.Core.LogLevel.Debug,
                "INFORMATION" or "INFO" => Logging.Core.LogLevel.Information,
                "WARNING" or "WARN" => Logging.Core.LogLevel.Warning,
                "ERROR" => Logging.Core.LogLevel.Error,
                "CRITICAL" or "FATAL" => Logging.Core.LogLevel.Critical,
                _ => Logging.Core.LogLevel.Trace
            };
        }

        private static void RegisterLoggingSinks(IServiceCollection services, IConfiguration configuration)
        {
            // Console sink
            services.Configure<Logging.Console.ConsoleOptions>(configuration.GetSection("JonjubNet:Logging:Console"));
            services.AddSingleton<Logging.Console.ConsoleLogSink>();
            services.AddSingleton<Logging.Core.Interfaces.ILogSink>(sp => sp.GetRequiredService<Logging.Console.ConsoleLogSink>());

            // Serilog sink
            services.Configure<Logging.Serilog.SerilogOptions>(configuration.GetSection("JonjubNet:Logging:Serilog"));
            services.AddSingleton<Logging.Serilog.SerilogLogSink>();
            services.AddSingleton<Logging.Core.Interfaces.ILogSink>(sp => sp.GetRequiredService<Logging.Serilog.SerilogLogSink>());

            // Kafka sink
            services.Configure<Logging.Kafka.KafkaOptions>(configuration.GetSection("JonjubNet:Logging:Kafka"));
            services.AddSingleton<Logging.Kafka.KafkaLogSink>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Logging.Kafka.KafkaOptions>>();
                var factory = sp.GetRequiredService<Shared.Kafka.KafkaProducerFactory>();
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Logging.Kafka.KafkaLogSink>>();
                return new Logging.Kafka.KafkaLogSink(options, factory, logger);
            });
            services.AddSingleton<Logging.Core.Interfaces.ILogSink>(sp => sp.GetRequiredService<Logging.Kafka.KafkaLogSink>());

            // HTTP sink
            services.Configure<Logging.Http.HttpOptions>(configuration.GetSection("JonjubNet:Logging:Http"));
            services.AddSingleton<Logging.Http.HttpLogSink>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Logging.Http.HttpOptions>>();
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Logging.Http.HttpLogSink>>();
                var httpClientFactory = sp.GetService<SecureHttpClientFactory>();
                var encryptionService = sp.GetService<EncryptionService>();
                return new Logging.Http.HttpLogSink(options, logger, httpClientFactory, encryptionService);
            });
            services.AddSingleton<Logging.Core.Interfaces.ILogSink>(sp => sp.GetRequiredService<Logging.Http.HttpLogSink>());

            // Elasticsearch sink
            services.Configure<Logging.Elasticsearch.ElasticsearchOptions>(configuration.GetSection("JonjubNet:Logging:Elasticsearch"));
            services.AddSingleton<Logging.Elasticsearch.ElasticsearchLogSink>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Logging.Elasticsearch.ElasticsearchOptions>>();
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Logging.Elasticsearch.ElasticsearchLogSink>>();
                var httpClientFactory = sp.GetService<SecureHttpClientFactory>();
                var encryptionService = sp.GetService<EncryptionService>();
                return new Logging.Elasticsearch.ElasticsearchLogSink(options, logger, httpClientFactory, encryptionService);
            });
            services.AddSingleton<Logging.Core.Interfaces.ILogSink>(sp => sp.GetRequiredService<Logging.Elasticsearch.ElasticsearchLogSink>());

            // OpenTelemetry sink
            services.Configure<Logging.OpenTelemetry.OTLLogOptions>(configuration.GetSection("JonjubNet:Logging:OpenTelemetry"));
            services.AddSingleton<Logging.OpenTelemetry.OTLLogSink>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Logging.OpenTelemetry.OTLLogOptions>>();
                var loggingOptions = sp.GetRequiredService<IOptions<Logging.Shared.Configuration.LoggingOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Logging.OpenTelemetry.OTLLogSink>>();
                var encryptionService = loggingOptions.Encryption.Enabled 
                    ? sp.GetService<EncryptionService>() 
                    : null;
                var secureHttpClientFactory = sp.GetService<SecureHttpClientFactory>();
                
                return new Logging.OpenTelemetry.OTLLogSink(
                    options,
                    logger,
                    httpClient: null,
                    encryptionService,
                    secureHttpClientFactory,
                    encryptInTransit: loggingOptions.Encryption.Enabled,
                    enableTls: true);
            });
            services.AddSingleton<Logging.Core.Interfaces.ILogSink>(sp => sp.GetRequiredService<Logging.OpenTelemetry.OTLLogSink>());
        }

        /// <summary>
        /// Registra el wrapper de métricas de Redis.
        /// El microservicio debe proporcionar una función para obtener el tenant code.
        /// </summary>
        public static IServiceCollection AddRedisMetrics(
            this IServiceCollection services,
            Func<IServiceProvider, Func<string?>>? getTenantCodeFactory = null,
            string? metricPrefix = null)
        {
            services.AddSingleton<Metrics.Shared.Redis.RedisMetricsWrapper>(serviceProvider =>
            {
                var metrics = serviceProvider.GetService<IMetricsClient>();
                var getTenantCode = getTenantCodeFactory?.Invoke(serviceProvider);
                var logger = serviceProvider.GetService<Microsoft.Extensions.Logging.ILogger<Metrics.Shared.Redis.RedisMetricsWrapper>>();
                return new Metrics.Shared.Redis.RedisMetricsWrapper(metrics, getTenantCode, logger, metricPrefix);
            });

            return services;
        }

        /// <summary>
        /// Registra helpers de métricas de base de datos.
        /// El microservicio debe proporcionar una función para obtener el tenant code.
        /// 
        /// NOTA: El interceptor de EF Core debe crearse en el microservicio usando DatabaseMetricsHelper.
        /// El componente base solo provee los helpers agnósticos de tecnología.
        /// </summary>
        public static IServiceCollection AddDatabaseMetrics(
            this IServiceCollection services,
            Func<IServiceProvider, Func<string?>>? getTenantCodeFactory = null,
            string? metricPrefix = null)
        {
            // Registrar helper como servicio para que pueda ser usado por interceptores/decoradores del microservicio
            services.AddSingleton(serviceProvider =>
            {
                var metrics = serviceProvider.GetService<IMetricsClient>();
                var getTenantCode = getTenantCodeFactory?.Invoke(serviceProvider);
                return new
                {
                    Metrics = metrics,
                    GetTenantCode = getTenantCode,
                    MetricPrefix = metricPrefix ?? "database"
                };
            });

            return services;
        }

        /// <summary>
        /// Agrega la infraestructura de tracing
        /// </summary>
        public static IServiceCollection AddJonjubNetTracing(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<Tracing.Shared.Configuration.TracingOptions>? configureOptions = null)
        {
            // Configurar opciones
            services.Configure<Tracing.Shared.Configuration.TracingOptions>(options =>
            {
                configuration.GetSection("JonjubNet:Tracing").Bind(options);
                configuration.GetSection("Tracing").Bind(options);
                configureOptions?.Invoke(options);
            });

            // Registrar servicios de encriptación (reutilizar de Shared)
            services.AddSingleton<EncryptionService>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Tracing.Shared.Configuration.TracingOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<EncryptionService>>();
                
                if (!string.IsNullOrEmpty(options.Encryption.EncryptionKeyBase64) && 
                    !string.IsNullOrEmpty(options.Encryption.EncryptionIVBase64))
                {
                    var key = Convert.FromBase64String(options.Encryption.EncryptionKeyBase64);
                    var iv = Convert.FromBase64String(options.Encryption.EncryptionIVBase64);
                    return new EncryptionService(key, iv, logger);
                }
                
                return new EncryptionService(logger);
            });

            // Registrar SecureHttpClientFactory
            services.AddSingleton<SecureHttpClientFactory>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Tracing.Shared.Configuration.TracingOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<SecureHttpClientFactory>>();
                
                return new SecureHttpClientFactory(
                    validateCertificates: options.Encryption.ValidateCertificates,
                    clientCertificate: null,
                    logger: logger);
            });

            // Registrar componentes Core
            services.AddSingleton<Tracing.Core.TraceRegistry>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Tracing.Shared.Configuration.TracingOptions>>().Value;
                var registry = new Tracing.Core.TraceRegistry();
                registry.MaxSize = options.RegistryCapacity;
                return registry;
            });

            services.AddSingleton<Tracing.Core.TraceScopeManager>();

            // Registrar TracingClient
            services.AddSingleton<Tracing.Core.Interfaces.ITracingClient>(sp =>
            {
                var registry = sp.GetRequiredService<Tracing.Core.TraceRegistry>();
                var scopeManager = sp.GetRequiredService<Tracing.Core.TraceScopeManager>();
                return new Tracing.Core.TracingClient(registry, scopeManager);
            });

            // Registrar Dead Letter Queue si está habilitada
            services.AddSingleton<Tracing.Core.Resilience.DeadLetterQueue>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Tracing.Shared.Configuration.TracingOptions>>().Value;
                if (!options.DeadLetterQueue.Enabled)
                {
                    return null!;
                }
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Tracing.Core.Resilience.DeadLetterQueue>>();
                var encryptionService = options.DeadLetterQueue.EncryptAtRest 
                    ? sp.GetService<EncryptionService>() 
                    : null;
                return new Tracing.Core.Resilience.DeadLetterQueue(
                    maxSize: options.DeadLetterQueue.MaxSize,
                    logger: logger,
                    encryptionService: encryptionService,
                    encryptAtRest: options.DeadLetterQueue.EncryptAtRest);
            });

            // Registrar RetryPolicy si está habilitada
            services.AddSingleton<Tracing.Core.Resilience.RetryPolicy>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Tracing.Shared.Configuration.TracingOptions>>().Value;
                if (!options.RetryPolicy.Enabled)
                {
                    return null!;
                }
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Tracing.Core.Resilience.RetryPolicy>>();
                return new Tracing.Core.Resilience.RetryPolicy(
                    maxRetries: options.RetryPolicy.MaxRetries,
                    initialDelay: TimeSpan.FromMilliseconds(options.RetryPolicy.InitialDelayMs),
                    backoffMultiplier: options.RetryPolicy.BackoffMultiplier,
                    jitterPercent: options.RetryPolicy.JitterPercent,
                    logger: logger);
            });

            // Registrar TraceFlushScheduler
            services.AddSingleton<Tracing.Core.TraceFlushScheduler>(sp =>
            {
                var registry = sp.GetRequiredService<Tracing.Core.TraceRegistry>();
                var sinks = sp.GetServices<Tracing.Core.Interfaces.ITraceSink>();
                var options = sp.GetRequiredService<IOptions<Tracing.Shared.Configuration.TracingOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Tracing.Core.TraceFlushScheduler>>();
                var deadLetterQueue = sp.GetService<Tracing.Core.Resilience.DeadLetterQueue>();
                var retryPolicy = sp.GetService<Tracing.Core.Resilience.RetryPolicy>();
                
                return new Tracing.Core.TraceFlushScheduler(
                    registry: registry,
                    sinks: sinks,
                    exportInterval: TimeSpan.FromMilliseconds(options.FlushIntervalMs),
                    logger: logger,
                    deadLetterQueue: deadLetterQueue,
                    retryPolicy: retryPolicy);
            });

            // Registrar sinks
            RegisterTracingSinks(services, configuration);

            // Registrar background service
            services.AddHostedService<TracingBackgroundService>();
            
            // Registrar TracingConfigWatcher
            services.AddHostedService<TracingConfigWatcher>();

            // Registrar configuración
            services.AddSingleton<Tracing.Shared.Configuration.TracingConfigurationManager>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Tracing.Shared.Configuration.TracingConfigurationManager>>();
                return new Tracing.Shared.Configuration.TracingConfigurationManager(config, logger);
            });

            return services;
        }

        private static void RegisterTracingSinks(IServiceCollection services, IConfiguration configuration)
        {
            // OpenTelemetry sink
            services.Configure<Tracing.OpenTelemetry.OTLTraceOptions>(configuration.GetSection("JonjubNet:Tracing:OpenTelemetry"));
            services.AddSingleton<Tracing.OpenTelemetry.OTLTraceExporter>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Tracing.OpenTelemetry.OTLTraceOptions>>();
                var tracingOptions = sp.GetRequiredService<IOptions<Tracing.Shared.Configuration.TracingOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Tracing.OpenTelemetry.OTLTraceExporter>>();
                var encryptionService = tracingOptions.Encryption.EnableInTransit 
                    ? sp.GetService<EncryptionService>() 
                    : null;
                var secureHttpClientFactory = sp.GetService<SecureHttpClientFactory>();
                
                return new Tracing.OpenTelemetry.OTLTraceExporter(
                    options,
                    logger,
                    httpClient: null,
                    encryptionService,
                    secureHttpClientFactory,
                    encryptInTransit: tracingOptions.Encryption.EnableInTransit,
                    enableTls: tracingOptions.Encryption.EnableTls);
            });
            services.AddSingleton<Tracing.Core.Interfaces.ITraceSink>(sp => sp.GetRequiredService<Tracing.OpenTelemetry.OTLTraceExporter>());

            // Kafka sink
            services.Configure<Tracing.Kafka.KafkaOptions>(configuration.GetSection("JonjubNet:Tracing:Kafka"));
            services.AddSingleton<Tracing.Kafka.KafkaTraceExporter>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Tracing.Kafka.KafkaOptions>>();
                var factory = sp.GetRequiredService<Shared.Kafka.KafkaProducerFactory>();
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Tracing.Kafka.KafkaTraceExporter>>();
                return new Tracing.Kafka.KafkaTraceExporter(options, factory, logger);
            });
            services.AddSingleton<Tracing.Core.Interfaces.ITraceSink>(sp => sp.GetRequiredService<Tracing.Kafka.KafkaTraceExporter>());

            // Elasticsearch sink
            services.Configure<Tracing.Elasticsearch.ElasticsearchOptions>(configuration.GetSection("JonjubNet:Tracing:Elasticsearch"));
            services.AddSingleton<Tracing.Elasticsearch.ElasticsearchTraceExporter>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Tracing.Elasticsearch.ElasticsearchOptions>>();
                var tracingOptions = sp.GetRequiredService<IOptions<Tracing.Shared.Configuration.TracingOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Tracing.Elasticsearch.ElasticsearchTraceExporter>>();
                var secureHttpClientFactory = tracingOptions.Encryption.EnableTls 
                    ? sp.GetService<SecureHttpClientFactory>() 
                    : null;
                var encryptionService = tracingOptions.Encryption.EnableInTransit 
                    ? sp.GetService<EncryptionService>() 
                    : null;
                
                return new Tracing.Elasticsearch.ElasticsearchTraceExporter(
                    options,
                    logger,
                    secureHttpClientFactory,
                    encryptionService);
            });
            services.AddSingleton<Tracing.Core.Interfaces.ITraceSink>(sp => sp.GetRequiredService<Tracing.Elasticsearch.ElasticsearchTraceExporter>());

            // HTTP sink
            services.Configure<Tracing.Http.HttpOptions>(configuration.GetSection("JonjubNet:Tracing:Http"));
            services.AddSingleton<Tracing.Http.HttpTraceExporter>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Tracing.Http.HttpOptions>>();
                var tracingOptions = sp.GetRequiredService<IOptions<Tracing.Shared.Configuration.TracingOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Tracing.Http.HttpTraceExporter>>();
                var secureHttpClientFactory = tracingOptions.Encryption.EnableTls 
                    ? sp.GetService<SecureHttpClientFactory>() 
                    : null;
                var encryptionService = tracingOptions.Encryption.EnableInTransit 
                    ? sp.GetService<EncryptionService>() 
                    : null;
                
                return new Tracing.Http.HttpTraceExporter(
                    options,
                    logger,
                    secureHttpClientFactory,
                    encryptionService);
            });
            services.AddSingleton<Tracing.Core.Interfaces.ITraceSink>(sp => sp.GetRequiredService<Tracing.Http.HttpTraceExporter>());
        }

        /// <summary>
        /// Registra los sinks con configuración de encriptación desde MetricsOptions
        /// </summary>
        private static void RegisterSinksWithEncryption(IServiceCollection services, IConfiguration configuration)
        {
            // Registrar OpenTelemetry sink con encriptación
            services.Configure<Metrics.OpenTelemetry.OTLOptions>(options =>
            {
                configuration.GetSection("Metrics:OpenTelemetry").Bind(options);
            });
            services.AddSingleton<Metrics.OpenTelemetry.OTLPExporter>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Metrics.OpenTelemetry.OTLOptions>>();
                var metricsOptions = sp.GetRequiredService<IOptions<MetricsOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Metrics.OpenTelemetry.OTLPExporter>>();
                var encryptionService = metricsOptions.Encryption.EnableInTransit 
                    ? sp.GetService<EncryptionService>() 
                    : null;
                var secureHttpClientFactory = metricsOptions.Encryption.EnableTls 
                    ? sp.GetService<SecureHttpClientFactory>() 
                    : null;
                
                return new Metrics.OpenTelemetry.OTLPExporter(
                    options,
                    logger,
                    httpClient: null,
                    encryptionService,
                    secureHttpClientFactory,
                    encryptInTransit: metricsOptions.Encryption.EnableInTransit,
                    enableTls: metricsOptions.Encryption.EnableTls);
            });
            services.AddSingleton<IMetricsSink>(sp => sp.GetRequiredService<Metrics.OpenTelemetry.OTLPExporter>());

            // Registrar InfluxDB sink con encriptación
            services.Configure<Metrics.InfluxDB.InfluxOptions>(options =>
            {
                configuration.GetSection("Metrics:InfluxDB").Bind(options);
            });
            services.AddSingleton<Metrics.InfluxDB.InfluxSink>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Metrics.InfluxDB.InfluxOptions>>();
                var metricsOptions = sp.GetRequiredService<IOptions<MetricsOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Metrics.InfluxDB.InfluxSink>>();
                var encryptionService = metricsOptions.Encryption.EnableInTransit 
                    ? sp.GetService<EncryptionService>() 
                    : null;
                var secureHttpClientFactory = metricsOptions.Encryption.EnableTls 
                    ? sp.GetService<SecureHttpClientFactory>() 
                    : null;
                
                return new Metrics.InfluxDB.InfluxSink(
                    options,
                    logger,
                    httpClient: null,
                    encryptionService,
                    secureHttpClientFactory,
                    encryptInTransit: metricsOptions.Encryption.EnableInTransit,
                    enableTls: metricsOptions.Encryption.EnableTls);
            });
            services.AddSingleton<IMetricsSink>(sp => sp.GetRequiredService<Metrics.InfluxDB.InfluxSink>());

            // Registrar Elasticsearch sink para Metrics
            services.Configure<Metrics.Elasticsearch.ElasticsearchOptions>(options =>
            {
                configuration.GetSection("Metrics:Elasticsearch").Bind(options);
            });
            services.AddSingleton<Metrics.Elasticsearch.ElasticsearchMetricsSink>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Metrics.Elasticsearch.ElasticsearchOptions>>();
                var metricsOptions = sp.GetRequiredService<IOptions<MetricsOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Metrics.Elasticsearch.ElasticsearchMetricsSink>>();
                var secureHttpClientFactory = metricsOptions.Encryption.EnableTls 
                    ? sp.GetService<SecureHttpClientFactory>() 
                    : null;
                var encryptionService = metricsOptions.Encryption.EnableInTransit 
                    ? sp.GetService<EncryptionService>() 
                    : null;
                
                return new Metrics.Elasticsearch.ElasticsearchMetricsSink(
                    options,
                    logger,
                    secureHttpClientFactory,
                    encryptionService);
            });
            services.AddSingleton<IMetricsSink>(sp => sp.GetRequiredService<Metrics.Elasticsearch.ElasticsearchMetricsSink>());

            // Registrar HTTP sink para Metrics
            services.Configure<Metrics.Http.HttpOptions>(options =>
            {
                configuration.GetSection("Metrics:Http").Bind(options);
            });
            services.AddSingleton<Metrics.Http.HttpMetricsSink>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<Metrics.Http.HttpOptions>>();
                var metricsOptions = sp.GetRequiredService<IOptions<MetricsOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Metrics.Http.HttpMetricsSink>>();
                var secureHttpClientFactory = metricsOptions.Encryption.EnableTls 
                    ? sp.GetService<SecureHttpClientFactory>() 
                    : null;
                var encryptionService = metricsOptions.Encryption.EnableInTransit 
                    ? sp.GetService<EncryptionService>() 
                    : null;
                
                return new Metrics.Http.HttpMetricsSink(
                    options,
                    logger,
                    secureHttpClientFactory,
                    encryptionService);
            });
            services.AddSingleton<IMetricsSink>(sp => sp.GetRequiredService<Metrics.Http.HttpMetricsSink>());
        }

        /// <summary>
        /// Agrega la infraestructura de observabilidad (correlación, middleware HTTP)
        /// </summary>
        public static IServiceCollection AddJonjubNetObservability(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<Configuration.ObservabilityOptions>? configureOptions = null)
        {
            // Configurar opciones
            services.Configure<Configuration.ObservabilityOptions>(options =>
            {
                configuration.GetSection("JonjubNet:Observability").Bind(options);
                configureOptions?.Invoke(options);
            });

            // Registrar CorrelationDelegatingHandler para HttpClient
            services.AddTransient<Http.CorrelationDelegatingHandler>();

            return services;
        }

        /// <summary>
        /// Agrega el middleware HTTP de observabilidad al pipeline
        /// </summary>
        public static IApplicationBuilder UseJonjubNetObservability(this IApplicationBuilder app)
        {
            return app.UseMiddleware<Http.ObservabilityHttpMiddleware>();
        }
    }
}
