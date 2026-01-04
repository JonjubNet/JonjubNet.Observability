using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Logging.Core.Resilience
{
    /// <summary>
    /// Dead Letter Queue para logs que fallan después de todos los reintentos
    /// Soporta encriptación en reposo opcional
    /// Similar a Metrics.Core.Resilience.DeadLetterQueue pero para logs
    /// </summary>
    public class DeadLetterQueue : IDisposable
    {
        private readonly ConcurrentQueue<FailedLog> _queue;
        private readonly ConcurrentQueue<EncryptedFailedLogWrapper>? _encryptedQueue;
        private readonly int _maxSize;
        private readonly ILogger<DeadLetterQueue>? _logger;
        private readonly IEncryptionService? _encryptionService;
        private readonly bool _encryptAtRest;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

        // Wrapper interno para evitar dependencia directa de Shared
        private class EncryptedFailedLogWrapper
        {
            public byte[] EncryptedData { get; set; } = Array.Empty<byte>();
            public string SinkName { get; set; } = string.Empty;
            public DateTime FailedAt { get; set; }
            public int RetryCount { get; set; }
        }

        // Interfaz para evitar dependencia circular
        private interface IEncryptionService
        {
            byte[] Encrypt(byte[] data);
            byte[] Decrypt(byte[] encryptedData);
        }

        // Wrapper para EncryptionService de Shared
        private class EncryptionServiceWrapper : IEncryptionService
        {
            private readonly object _encryptionService;

            public EncryptionServiceWrapper(object encryptionService)
            {
                _encryptionService = encryptionService;
            }

            public byte[] Encrypt(byte[] data)
            {
                // Usar reflexión para evitar dependencia directa
                var encryptMethod = _encryptionService.GetType().GetMethod("Encrypt", new[] { typeof(byte[]) });
                return (byte[])encryptMethod!.Invoke(_encryptionService, new object[] { data })!;
            }

            public byte[] Decrypt(byte[] encryptedData)
            {
                var decryptMethod = _encryptionService.GetType().GetMethod("Decrypt", new[] { typeof(byte[]) });
                return (byte[])decryptMethod!.Invoke(_encryptionService, new object[] { encryptedData })!;
            }
        }

        public DeadLetterQueue(
            int maxSize = 10000,
            ILogger<DeadLetterQueue>? logger = null,
            object? encryptionService = null,
            bool encryptAtRest = false)
        {
            _maxSize = maxSize;
            _logger = logger;
            _encryptAtRest = encryptAtRest;
            
            if (encryptAtRest && encryptionService != null)
            {
                _encryptionService = new EncryptionServiceWrapper(encryptionService);
                _encryptedQueue = new ConcurrentQueue<EncryptedFailedLogWrapper>();
            }
            
            _queue = new ConcurrentQueue<FailedLog>();
        }

        /// <summary>
        /// Agrega un log fallido a la DLQ
        /// Si la encriptación en reposo está habilitada, encripta los datos antes de almacenarlos
        /// </summary>
        public bool Enqueue(FailedLog failedLog)
        {
            if (_encryptAtRest && _encryptionService != null && _encryptedQueue != null)
            {
                // Encriptar y almacenar en cola encriptada
                if (_encryptedQueue.Count >= _maxSize)
                {
                    if (!_encryptedQueue.TryDequeue(out _))
                    {
                        _logger?.LogWarning("Encrypted Dead Letter Queue is full, dropping oldest log");
                    }
                }

                try
                {
                    // Serializar a JSON y encriptar
                    var json = JsonSerializer.Serialize(failedLog, JsonOptions);
                    var jsonBytes = Encoding.UTF8.GetBytes(json);
                    var encryptedData = _encryptionService.Encrypt(jsonBytes);

                    var encrypted = new EncryptedFailedLogWrapper
                    {
                        EncryptedData = encryptedData,
                        SinkName = failedLog.SinkName,
                        FailedAt = failedLog.FailedAt,
                        RetryCount = failedLog.RetryCount
                    };

                    _encryptedQueue.Enqueue(encrypted);
                    _logger?.LogWarning(
                        "Log {LogMessage} added to encrypted DLQ after {RetryCount} retries. Error: {Error}",
                        failedLog.LogEntry.Message,
                        failedLog.RetryCount,
                        failedLog.LastException?.Message);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to encrypt log for DLQ, storing unencrypted");
                    // Fallback: almacenar sin encriptar
                    _queue.Enqueue(failedLog);
                }
            }
            else
            {
                // Almacenar sin encriptar
                if (_queue.Count >= _maxSize)
                {
                    // Intentar remover el más antiguo si está lleno
                    if (!_queue.TryDequeue(out _))
                    {
                        _logger?.LogWarning("Dead Letter Queue is full, dropping oldest log");
                    }
                }

                _queue.Enqueue(failedLog);
                _logger?.LogWarning(
                    "Log {LogMessage} added to DLQ after {RetryCount} retries. Error: {Error}",
                    failedLog.LogEntry.Message,
                    failedLog.RetryCount,
                    failedLog.LastException?.Message);
            }

            return true;
        }

        /// <summary>
        /// Intenta obtener un log fallido de la DLQ
        /// Si la encriptación en reposo está habilitada, desencripta los datos antes de retornarlos
        /// </summary>
        public bool TryDequeue(out FailedLog? failedLog)
        {
            failedLog = null;

            if (_encryptAtRest && _encryptionService != null && _encryptedQueue != null)
            {
                // Desencriptar desde cola encriptada
                if (_encryptedQueue.TryDequeue(out var encrypted))
                {
                    try
                    {
                        var decryptedBytes = _encryptionService.Decrypt(encrypted.EncryptedData);
                        var json = Encoding.UTF8.GetString(decryptedBytes);
                        failedLog = JsonSerializer.Deserialize<FailedLog>(json, JsonOptions);
                        return failedLog != null;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to decrypt log from DLQ");
                        return false;
                    }
                }
                return false;
            }
            else
            {
                // Retornar desde cola normal
                return _queue.TryDequeue(out failedLog);
            }
        }

        /// <summary>
        /// Obtiene todos los logs fallidos (para procesamiento batch)
        /// Optimizado: usa pooling para reducir allocations
        /// Si la encriptación en reposo está habilitada, desencripta los datos
        /// </summary>
        public IReadOnlyList<FailedLog> GetAll()
        {
            var result = new List<FailedLog>();

            if (_encryptAtRest && _encryptionService != null && _encryptedQueue != null)
            {
                // Desencriptar desde cola encriptada
                result = new List<FailedLog>(_encryptedQueue.Count);
                while (_encryptedQueue.TryDequeue(out var encrypted))
                {
                    try
                    {
                        var decryptedBytes = _encryptionService.Decrypt(encrypted.EncryptedData);
                        var json = Encoding.UTF8.GetString(decryptedBytes);
                        var failedLog = JsonSerializer.Deserialize<FailedLog>(json, JsonOptions);
                        if (failedLog != null)
                        {
                            result.Add(failedLog);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to decrypt log from DLQ during GetAll");
                    }
                }
            }
            else
            {
                // Retornar desde cola normal
                result = new List<FailedLog>(_queue.Count);
                while (_queue.TryDequeue(out var failedLog))
                {
                    result.Add(failedLog);
                }
            }

            return result;
        }

        /// <summary>
        /// Obtiene el conteo actual de logs en la DLQ
        /// </summary>
        public int Count => _encryptAtRest && _encryptedQueue != null 
            ? _encryptedQueue.Count 
            : _queue.Count;

        /// <summary>
        /// Limpia todos los logs de la DLQ
        /// </summary>
        public void Clear()
        {
            if (_encryptAtRest && _encryptedQueue != null)
            {
                while (_encryptedQueue.TryDequeue(out _)) { }
            }
            else
            {
                while (_queue.TryDequeue(out _)) { }
            }
            _logger?.LogInformation("Dead Letter Queue cleared");
        }

        /// <summary>
        /// Obtiene estadísticas de la DLQ
        /// </summary>
        public DeadLetterQueueStats GetStats()
        {
            var count = _encryptAtRest && _encryptedQueue != null 
                ? _encryptedQueue.Count 
                : _queue.Count;
            
            return new DeadLetterQueueStats
            {
                Count = count,
                MaxSize = _maxSize,
                UtilizationPercent = _maxSize > 0 ? (double)count / _maxSize * 100 : 0
            };
        }

        public void Dispose()
        {
            // ConcurrentQueue no necesita dispose, pero limpiamos la cola
            Clear();
        }
    }

    /// <summary>
    /// Representa un log que falló después de todos los reintentos
    /// </summary>
    public class FailedLog
    {
        public StructuredLogEntry LogEntry { get; set; } = null!;
        public string SinkName { get; set; } = string.Empty;
        public int RetryCount { get; set; }
        public Exception? LastException { get; set; }
        public DateTime FailedAt { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }

        public FailedLog(
            StructuredLogEntry logEntry,
            string sinkName,
            int retryCount,
            Exception? lastException = null,
            Dictionary<string, string>? metadata = null)
        {
            LogEntry = logEntry;
            SinkName = sinkName;
            RetryCount = retryCount;
            LastException = lastException;
            FailedAt = DateTime.UtcNow;
            Metadata = metadata;
        }
    }

    /// <summary>
    /// Estadísticas de la Dead Letter Queue
    /// </summary>
    public class DeadLetterQueueStats
    {
        public int Count { get; set; }
        public int MaxSize { get; set; }
        public double UtilizationPercent { get; set; }
    }
}

