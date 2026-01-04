namespace JonjubNet.Observability.Shared.Kafka
{
    /// <summary>
    /// Interfaz para el productor de Kafka
    /// Permite desacoplar la lógica de aplicación de la implementación de Kafka
    /// Común para Metrics y Logging
    /// </summary>
    public interface IKafkaProducer
    {
        /// <summary>
        /// Indica si el producer está habilitado
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Envía un mensaje a Kafka
        /// </summary>
        /// <param name="message">Mensaje a enviar (JSON string)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Task que representa la operación asíncrona</returns>
        Task SendAsync(string message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Envía un batch de mensajes a Kafka
        /// </summary>
        /// <param name="messages">Mensajes a enviar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Task que representa la operación asíncrona</returns>
        Task SendBatchAsync(IEnumerable<string> messages, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interfaz extendida para productores de Kafka que soportan headers
    /// Permite propagación de CorrelationId en headers de Kafka
    /// </summary>
    public interface IKafkaProducerWithHeaders : IKafkaProducer
    {
        /// <summary>
        /// Envía un mensaje a Kafka con headers personalizados
        /// </summary>
        /// <param name="message">Mensaje a enviar (JSON string)</param>
        /// <param name="headers">Headers adicionales (ej: X-Correlation-Id)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Task que representa la operación asíncrona</returns>
        Task SendAsync(string message, Dictionary<string, string>? headers, CancellationToken cancellationToken = default);

        /// <summary>
        /// Envía un batch de mensajes a Kafka con headers personalizados
        /// </summary>
        /// <param name="messages">Mensajes a enviar</param>
        /// <param name="headers">Headers adicionales (ej: X-Correlation-Id)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Task que representa la operación asíncrona</returns>
        Task SendBatchAsync(IEnumerable<string> messages, Dictionary<string, string>? headers, CancellationToken cancellationToken = default);
    }
}

