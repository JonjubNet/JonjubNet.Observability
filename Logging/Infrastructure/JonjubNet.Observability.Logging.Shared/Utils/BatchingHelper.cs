using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Logging.Shared.Utils
{
    /// <summary>
    /// Helper para batching inteligente de logs
    /// Agrupa logs por tiempo/volumen para optimizar el envío
    /// </summary>
    public class BatchingHelper<T>
    {
        private readonly int _maxBatchSize;
        private readonly TimeSpan _maxBatchAge;
        private readonly ILogger<BatchingHelper<T>>? _logger;
        private readonly List<BatchItem<T>> _currentBatch = new();
        private DateTime _batchStartTime = DateTime.UtcNow;
        private readonly object _lock = new();

        public BatchingHelper(
            int maxBatchSize = 100,
            TimeSpan? maxBatchAge = null,
            ILogger<BatchingHelper<T>>? logger = null)
        {
            _maxBatchSize = maxBatchSize;
            _maxBatchAge = maxBatchAge ?? TimeSpan.FromSeconds(5);
            _logger = logger;
        }

        /// <summary>
        /// Agrega un item al batch actual
        /// </summary>
        public void Add(T item)
        {
            lock (_lock)
            {
                _currentBatch.Add(new BatchItem<T>
                {
                    Item = item,
                    AddedAt = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Verifica si el batch está listo para ser enviado
        /// </summary>
        public bool IsBatchReady()
        {
            lock (_lock)
            {
                // Listo si alcanzó el tamaño máximo
                if (_currentBatch.Count >= _maxBatchSize)
                {
                    return true;
                }

                // Listo si ha pasado el tiempo máximo
                if (DateTime.UtcNow - _batchStartTime >= _maxBatchAge)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Obtiene el batch actual y lo limpia
        /// </summary>
        public List<T> GetBatchAndClear()
        {
            lock (_lock)
            {
                var items = _currentBatch.Select(bi => bi.Item).ToList();
                _currentBatch.Clear();
                _batchStartTime = DateTime.UtcNow;
                return items;
            }
        }

        /// <summary>
        /// Obtiene el batch actual sin limpiarlo
        /// </summary>
        public List<T> GetBatch()
        {
            lock (_lock)
            {
                return _currentBatch.Select(bi => bi.Item).ToList();
            }
        }

        /// <summary>
        /// Limpia el batch actual
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _currentBatch.Clear();
                _batchStartTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Obtiene el tamaño actual del batch
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _currentBatch.Count;
                }
            }
        }

        private class BatchItem<TItem>
        {
            public TItem Item { get; set; } = default!;
            public DateTime AddedAt { get; set; }
        }
    }
}

