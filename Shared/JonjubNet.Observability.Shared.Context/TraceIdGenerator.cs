using System.Security.Cryptography;

namespace JonjubNet.Observability.Shared.Context
{
    /// <summary>
    /// Generador de TraceID y SpanID (128 bits y 64 bits respectivamente)
    /// Optimizado: reutiliza lógica, thread-safe, sin allocations innecesarias
    /// </summary>
    public static class TraceIdGenerator
    {
        // String interning para valores comunes (optimización GC)
        private static readonly string EmptyString = string.Empty;

        /// <summary>
        /// Genera un Trace ID único (128 bits, formato hexadecimal)
        /// Compatible con OpenTelemetry y W3C Trace Context
        /// </summary>
        public static string GenerateTraceId()
        {
            var bytes = new byte[16];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        /// <summary>
        /// Genera un Span ID único (64 bits, formato hexadecimal)
        /// Compatible con OpenTelemetry y W3C Trace Context
        /// </summary>
        public static string GenerateSpanId()
        {
            var bytes = new byte[8];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        /// <summary>
        /// Genera un Correlation ID único (128 bits, formato hexadecimal)
        /// Similar a TraceID pero con nombre semántico diferente
        /// </summary>
        public static string GenerateCorrelationId()
        {
            return GenerateTraceId(); // Mismo formato que TraceID
        }

        /// <summary>
        /// Genera un Request ID único (128 bits, formato hexadecimal)
        /// Similar a TraceID pero con nombre semántico diferente
        /// </summary>
        public static string GenerateRequestId()
        {
            return GenerateTraceId(); // Mismo formato que TraceID
        }
    }
}

