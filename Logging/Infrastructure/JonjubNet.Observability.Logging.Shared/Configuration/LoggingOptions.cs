namespace JonjubNet.Observability.Logging.Shared.Configuration
{
    /// <summary>
    /// Opciones principales de configuración de logging
    /// Similar a MetricsOptions pero para logs
    /// </summary>
    public class LoggingOptions
    {
        /// <summary>
        /// Habilitar logging globalmente
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Nombre del servicio
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Entorno de ejecución
        /// </summary>
        public string Environment { get; set; } = "Development";

        /// <summary>
        /// Versión del servicio
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Capacidad máxima del Registry
        /// </summary>
        public int RegistryCapacity { get; set; } = 10000;

        /// <summary>
        /// Tamaño del batch para flushing
        /// </summary>
        public int BatchSize { get; set; } = 200;

        /// <summary>
        /// Intervalo de flush en milisegundos
        /// </summary>
        public int FlushIntervalMs { get; set; } = 1000;

        /// <summary>
        /// Nivel mínimo de log
        /// </summary>
        public string MinLevel { get; set; } = "Trace";

        /// <summary>
        /// Configuración de Dead Letter Queue
        /// </summary>
        public DeadLetterQueueOptions DeadLetterQueue { get; set; } = new();

        /// <summary>
        /// Configuración de Retry Policy
        /// </summary>
        public RetryPolicyOptions RetryPolicy { get; set; } = new();

        /// <summary>
        /// Configuración de Circuit Breaker
        /// </summary>
        public CircuitBreakerConfigurationOptions CircuitBreaker { get; set; } = new();

        /// <summary>
        /// Configuración de encriptación
        /// </summary>
        public EncryptionOptions Encryption { get; set; } = new();

        /// <summary>
        /// Configuración de sampling
        /// </summary>
        public SamplingOptions Sampling { get; set; } = new();

        /// <summary>
        /// Configuración de sanitización de datos
        /// </summary>
        public DataSanitizationOptions DataSanitization { get; set; } = new();
    }

    /// <summary>
    /// Opciones para Dead Letter Queue
    /// </summary>
    public class DeadLetterQueueOptions
    {
        /// <summary>
        /// Habilitar Dead Letter Queue
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Tamaño máximo de la DLQ
        /// </summary>
        public int MaxSize { get; set; } = 10000;

        /// <summary>
        /// Habilitar encriptación en reposo
        /// </summary>
        public bool EncryptAtRest { get; set; } = false;
    }

    /// <summary>
    /// Opciones para Retry Policy
    /// </summary>
    public class RetryPolicyOptions
    {
        /// <summary>
        /// Habilitar retry policy
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Número máximo de reintentos
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Delay inicial en milisegundos
        /// </summary>
        public int InitialDelayMs { get; set; } = 100;

        /// <summary>
        /// Multiplicador de backoff exponencial
        /// </summary>
        public double BackoffMultiplier { get; set; } = 2.0;

        /// <summary>
        /// Porcentaje de jitter (0.0 a 1.0)
        /// </summary>
        public double JitterPercent { get; set; } = 0.1;
    }

    /// <summary>
    /// Opciones de configuración para Circuit Breaker
    /// </summary>
    public class CircuitBreakerConfigurationOptions
    {
        /// <summary>
        /// Habilitar circuit breakers por sink individual
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Opciones por defecto para circuit breakers
        /// </summary>
        public CircuitBreakerDefaultOptions Default { get; set; } = new();

        /// <summary>
        /// Opciones específicas por sink (key = nombre del sink)
        /// </summary>
        public Dictionary<string, CircuitBreakerSinkOptions> Sinks { get; set; } = new();
    }

    /// <summary>
    /// Opciones por defecto para circuit breakers
    /// </summary>
    public class CircuitBreakerDefaultOptions
    {
        /// <summary>
        /// Número de fallos antes de abrir el circuit breaker
        /// </summary>
        public int FailureThreshold { get; set; } = 5;

        /// <summary>
        /// Duración que el circuit breaker permanece abierto (en segundos)
        /// </summary>
        public int OpenDurationSeconds { get; set; } = 30;
    }

    /// <summary>
    /// Opciones de circuit breaker para un sink específico
    /// </summary>
    public class CircuitBreakerSinkOptions
    {
        /// <summary>
        /// Habilitar circuit breaker para este sink
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Número de fallos antes de abrir el circuit breaker
        /// </summary>
        public int FailureThreshold { get; set; } = 5;

        /// <summary>
        /// Duración que el circuit breaker permanece abierto (en segundos)
        /// </summary>
        public int OpenDurationSeconds { get; set; } = 30;
    }

    /// <summary>
    /// Opciones de encriptación
    /// </summary>
    public class EncryptionOptions
    {
        /// <summary>
        /// Habilitar encriptación
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Clave de encriptación (base64)
        /// </summary>
        public string? EncryptionKey { get; set; }
    }

    /// <summary>
    /// Opciones de sampling
    /// </summary>
    public class SamplingOptions
    {
        /// <summary>
        /// Habilitar sampling
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Probabilidad de sampling (0.0 a 1.0)
        /// </summary>
        public double Probability { get; set; } = 1.0;

        /// <summary>
        /// Rate limit (logs por segundo)
        /// </summary>
        public int RateLimitPerSecond { get; set; } = 1000;
    }

    /// <summary>
    /// Opciones de sanitización de datos
    /// </summary>
    public class DataSanitizationOptions
    {
        /// <summary>
        /// Habilitar sanitización de datos
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Patrones de datos sensibles a enmascarar (regex)
        /// </summary>
        public List<string> SensitiveDataPatterns { get; set; } = new()
        {
            @"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", // Tarjetas de crédito
            @"\b\d{3}-\d{2}-\d{4}\b", // SSN
            @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b" // Emails
        };

        /// <summary>
        /// String de reemplazo para datos sensibles
        /// </summary>
        public string MaskString { get; set; } = "***";
    }
}

