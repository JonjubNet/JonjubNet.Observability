namespace JonjubNet.Observability.Metrics.Shared.Configuration
{
    /// <summary>
    /// Opciones principales de configuración de métricas
    /// </summary>
    public class MetricsOptions
    {
        /// <summary>
        /// Habilitar métricas globalmente
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
        /// Capacidad del bus de métricas
        /// </summary>
        public int QueueCapacity { get; set; } = 10000;

        /// <summary>
        /// Tamaño del batch para flushing
        /// </summary>
        public int BatchSize { get; set; } = 200;

        /// <summary>
        /// Intervalo de flush en milisegundos
        /// </summary>
        public int FlushIntervalMs { get; set; } = 1000;

        /// <summary>
        /// Modo de operación
        /// </summary>
        public MetricsMode Mode { get; set; } = MetricsMode.InProcess;

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
        /// Habilitar procesamiento automático de DLQ (reintentos periódicos)
        /// </summary>
        public bool EnableAutoProcessing { get; set; } = true;

        /// <summary>
        /// Intervalo de procesamiento de DLQ en milisegundos
        /// </summary>
        public int ProcessingIntervalMs { get; set; } = 60000; // 1 minuto por defecto

        /// <summary>
        /// Número máximo de reintentos para métricas en DLQ
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;
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
        public double JitterPercent { get; set; } = 0.1; // 10% de jitter
    }

    /// <summary>
    /// Modo de operación de métricas
    /// </summary>
    public enum MetricsMode
    {
        /// <summary>
        /// In-process: métricas en el mismo proceso
        /// </summary>
        InProcess,

        /// <summary>
        /// Sidecar: métricas en proceso separado
        /// </summary>
        Sidecar
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
        public int? FailureThreshold { get; set; }

        /// <summary>
        /// Duración que el circuit breaker permanece abierto (en segundos)
        /// </summary>
        public int? OpenDurationSeconds { get; set; }
    }

    /// <summary>
    /// Opciones de encriptación para métricas
    /// </summary>
    public class EncryptionOptions
    {
        /// <summary>
        /// Habilitar encriptación en tránsito (para sinks HTTP)
        /// </summary>
        public bool EnableInTransit { get; set; } = false;

        /// <summary>
        /// Habilitar encriptación en reposo (para DLQ y almacenamiento)
        /// </summary>
        public bool EnableAtRest { get; set; } = false;

        /// <summary>
        /// Clave de encriptación en Base64 (si está vacía, se genera automáticamente)
        /// </summary>
        public string? EncryptionKeyBase64 { get; set; }

        /// <summary>
        /// IV de encriptación en Base64 (si está vacío, se genera automáticamente)
        /// </summary>
        public string? EncryptionIVBase64 { get; set; }

        /// <summary>
        /// Habilitar TLS/SSL para conexiones HTTP (encriptación en tránsito a nivel de protocolo)
        /// </summary>
        public bool EnableTls { get; set; } = true;

        /// <summary>
        /// Validar certificados SSL
        /// </summary>
        public bool ValidateCertificates { get; set; } = true;
    }
}

