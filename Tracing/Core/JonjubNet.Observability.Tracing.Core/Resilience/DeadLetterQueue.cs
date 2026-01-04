using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Tracing.Core.Resilience
{
    /// <summary>
    /// Dead Letter Queue para spans que fallan después de todos los reintentos
    /// Soporta encriptación en reposo opcional
    /// Similar a Logging.Core.Resilience.DeadLetterQueue pero para spans
    /// </summary>
    public class DeadLetterQueue : IDisposable
    {
        private readonly ConcurrentQueue<FailedSpan> _queue;
        private readonly ConcurrentQueue<EncryptedFailedSpanWrapper>? _encryptedQueue;
        private readonly int _maxSize;
        private readonly ILogger<DeadLetterQueue>? _logger;
        private readonly IEncryptionService? _encryptionService;
        private readonly bool _encryptAtRest;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

        // Wrapper interno para evitar dependencia directa de Shared
        private class EncryptedFailedSpanWrapper
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
                _encryptedQueue = new ConcurrentQueue<EncryptedFailedSpanWrapper>();
            }
            
            _queue = new ConcurrentQueue<FailedSpan>();
        }

        /// <summary>
        /// Agrega un span fallido a la DLQ
        /// Si la encriptación en reposo está habilitada, encripta los datos antes de almacenarlos
        /// </summary>
        public bool Enqueue(FailedSpan failedSpan)
        {
            if (_encryptAtRest && _encryptionService != null && _encryptedQueue != null)
            {
                // Encriptar y almacenar en cola encriptada
                if (_encryptedQueue.Count >= _maxSize)
                {
                    if (!_encryptedQueue.TryDequeue(out _))
                    {
                        _logger?.LogWarning("Encrypted Dead Letter Queue is full, dropping oldest span");
                    }
                }

                try
                {
                    // Serializar a JSON y encriptar
                    var json = JsonSerializer.Serialize(failedSpan, JsonOptions);
                    var jsonBytes = Encoding.UTF8.GetBytes(json);
                    var encryptedData = _encryptionService.Encrypt(jsonBytes);

                    var encrypted = new EncryptedFailedSpanWrapper
                    {
                        EncryptedData = encryptedData,
                        SinkName = failedSpan.SinkName,
                        FailedAt = failedSpan.FailedAt,
                        RetryCount = failedSpan.RetryCount
                    };

                    _encryptedQueue.Enqueue(encrypted);
                    _logger?.LogWarning(
                        "Span {SpanOperation} added to encrypted DLQ after {RetryCount} retries. Error: {Error}",
                        failedSpan.Span.OperationName,
                        failedSpan.RetryCount,
                        failedSpan.LastException?.Message);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to encrypt span for DLQ, storing unencrypted");
                    // Fallback: almacenar sin encriptar
                    _queue.Enqueue(failedSpan);
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
                        _logger?.LogWarning("Dead Letter Queue is full, dropping oldest span");
                    }
                }

                _queue.Enqueue(failedSpan);
                _logger?.LogWarning(
                    "Span {SpanOperation} added to DLQ after {RetryCount} retries. Error: {Error}",
                    failedSpan.Span.OperationName,
                    failedSpan.RetryCount,
                    failedSpan.LastException?.Message);
            }

            return true;
        }

        /// <summary>
        /// Intenta obtener un span fallido de la DLQ
        /// Si la encriptación en reposo está habilitada, desencripta los datos antes de retornarlos
        /// </summary>
        public bool TryDequeue(out FailedSpan? failedSpan)
        {
            failedSpan = null;

            if (_encryptAtRest && _encryptionService != null && _encryptedQueue != null)
            {
                // Desencriptar desde cola encriptada
                if (_encryptedQueue.TryDequeue(out var encrypted))
                {
                    try
                    {
                        var decryptedBytes = _encryptionService.Decrypt(encrypted.EncryptedData);
                        var json = Encoding.UTF8.GetString(decryptedBytes);
                        failedSpan = JsonSerializer.Deserialize<FailedSpan>(json, JsonOptions);
                        return failedSpan != null;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to decrypt span from DLQ");
                        return false;
                    }
                }
                return false;
            }
            else
            {
                // Retornar desde cola normal
                return _queue.TryDequeue(out failedSpan);
            }
        }

        /// <summary>
        /// Obtiene todos los spans fallidos (para procesamiento batch)
        /// Optimizado: usa pooling para reducir allocations
        /// Si la encriptación en reposo está habilitada, desencripta los datos
        /// </summary>
        public IReadOnlyList<FailedSpan> GetAll()
        {
            var result = new List<FailedSpan>();

            if (_encryptAtRest && _encryptionService != null && _encryptedQueue != null)
            {
                // Desencriptar desde cola encriptada
                result = new List<FailedSpan>(_encryptedQueue.Count);
                while (_encryptedQueue.TryDequeue(out var encrypted))
                {
                    try
                    {
                        var decryptedBytes = _encryptionService.Decrypt(encrypted.EncryptedData);
                        var json = Encoding.UTF8.GetString(decryptedBytes);
                        var failedSpan = JsonSerializer.Deserialize<FailedSpan>(json, JsonOptions);
                        if (failedSpan != null)
                        {
                            result.Add(failedSpan);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to decrypt span from DLQ during GetAll");
                    }
                }
            }
            else
            {
                // Retornar desde cola normal
                result = new List<FailedSpan>(_queue.Count);
                while (_queue.TryDequeue(out var failedSpan))
                {
                    result.Add(failedSpan);
                }
            }

            return result;
        }

        /// <summary>
        /// Obtiene el conteo actual de spans en la DLQ
        /// </summary>
        public int Count => _encryptAtRest && _encryptedQueue != null 
            ? _encryptedQueue.Count 
            : _queue.Count;

        /// <summary>
        /// Limpia todos los spans de la DLQ
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
    /// Representa un span que falló después de todos los reintentos
    /// </summary>
    public class FailedSpan
    {
        public Span Span { get; set; } = null!;
        public string SinkName { get; set; } = string.Empty;
        public int RetryCount { get; set; }
        public Exception? LastException { get; set; }
        public DateTime FailedAt { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }

        public FailedSpan(
            Span span,
            string sinkName,
            int retryCount,
            Exception? lastException = null,
            Dictionary<string, string>? metadata = null)
        {
            Span = span;
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
