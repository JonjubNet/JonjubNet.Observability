namespace JonjubNet.Observability.Metrics.Shared.Utils
{
    /// <summary>
    /// Extensiones para TimeProvider
    /// </summary>
    public static class TimeProviderExtensions
    {
        /// <summary>
        /// Obtiene el timestamp actual en milisegundos desde epoch
        /// </summary>
        public static long GetTimestampMs(this TimeProvider timeProvider)
        {
            return timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Obtiene el timestamp actual en segundos desde epoch
        /// </summary>
        public static long GetTimestampSeconds(this TimeProvider timeProvider)
        {
            return timeProvider.GetUtcNow().ToUnixTimeSeconds();
        }
    }
}

