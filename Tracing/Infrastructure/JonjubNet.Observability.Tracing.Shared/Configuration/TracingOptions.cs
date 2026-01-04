namespace JonjubNet.Observability.Tracing.Shared.Configuration
{
    /// <summary>
    /// Opciones principales de configuración de tracing
    /// Similar a LoggingOptions pero para traces/spans
    /// </summary>
    public class TracingOptions
    {
        /// <summary>
        /// Habilitar tracing globalmente
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
        /// Configuración de Dead Letter Queue
        /// </summary>
        public DeadLetterQueueOptions DeadLetterQueue { get; set; } = new();

        /// <summary>
        /// Configuración de Retry Policy
        /// </summary>
        public RetryPolicyOptions RetryPolicy { get; set; } = new();

        /// <summary>
        /// Configuración de encriptación
        /// </summary>
        public EncryptionOptions Encryption { get; set; } = new();

        /// <summary>
        /// Configuración de sampling
        /// </summary>
        public SamplingOptions Sampling { get; set; } = new();
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

        /// <summary>
        /// Habilitar procesamiento automático de la DLQ
        /// </summary>
        public bool EnableAutoProcessing { get; set; } = true;

        /// <summary>
        /// Intervalo de procesamiento de la DLQ en milisegundos
        /// </summary>
        public int ProcessingIntervalMs { get; set; } = 60000; // 1 minuto
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
    /// Opciones de encriptación
    /// </summary>
    public class EncryptionOptions
    {
        /// <summary>
        /// Habilitar encriptación en tránsito
        /// </summary>
        public bool EnableInTransit { get; set; } = false;

        /// <summary>
        /// Habilitar TLS/SSL
        /// </summary>
        public bool EnableTls { get; set; } = true;

        /// <summary>
        /// Validar certificados
        /// </summary>
        public bool ValidateCertificates { get; set; } = true;

        /// <summary>
        /// Clave de encriptación (base64)
        /// </summary>
        public string? EncryptionKeyBase64 { get; set; }

        /// <summary>
        /// IV de encriptación (base64)
        /// </summary>
        public string? EncryptionIVBase64 { get; set; }
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
        /// Rate limit (spans por segundo)
        /// </summary>
        public int RateLimitPerSecond { get; set; } = 1000;
    }
}
