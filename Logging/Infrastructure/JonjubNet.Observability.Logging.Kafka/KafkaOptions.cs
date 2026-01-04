namespace JonjubNet.Observability.Logging.Kafka
{
    /// <summary>
    /// Opciones de configuración para el sink de Kafka
    /// </summary>
    public class KafkaOptions
    {
        /// <summary>
        /// Indica si el sink está habilitado
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Bootstrap servers para conexión nativa (tiene prioridad)
        /// </summary>
        public string? BootstrapServers { get; set; }

        /// <summary>
        /// URL para REST Proxy o Webhook
        /// </summary>
        public string? ProducerUrl { get; set; }

        /// <summary>
        /// Topic de Kafka
        /// </summary>
        public string Topic { get; set; } = "logs";

        /// <summary>
        /// Si es true, usa Webhook; si es false, usa REST Proxy
        /// </summary>
        public bool UseWebhook { get; set; } = false;

        /// <summary>
        /// Headers adicionales para webhook
        /// </summary>
        public Dictionary<string, string>? Headers { get; set; }

        /// <summary>
        /// Configuración adicional para producer nativo
        /// </summary>
        public Dictionary<string, string>? AdditionalConfig { get; set; }

        /// <summary>
        /// Tamaño de batch para envío agrupado
        /// </summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// Timeout en segundos
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;
    }
}

