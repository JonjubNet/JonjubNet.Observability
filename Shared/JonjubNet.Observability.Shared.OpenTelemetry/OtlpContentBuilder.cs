using System.Text.Json;
using JonjubNet.Observability.Shared.Security;
using JonjubNet.Observability.Shared.Utils;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Shared.OpenTelemetry
{
    /// <summary>
    /// Builder para crear HttpContent con compresión y encriptación OTLP
    /// Compartido entre Metrics y Logging
    /// </summary>
    public static class OtlpContentBuilder
    {
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptionsCache.GetDefault();

        /// <summary>
        /// Crea HttpContent con compresión y encriptación si está habilitada usando OtlpOptions
        /// </summary>
        public static HttpContent CreateContent(
            object otlpPayload,
            OtlpOptions options,
            EncryptionService? encryptionService = null,
            bool encryptInTransit = false,
            ILogger? logger = null)
        {
            return CreateContent(
                otlpPayload,
                options.EnableCompression,
                encryptionService,
                encryptInTransit,
                logger);
        }

        /// <summary>
        /// Crea HttpContent con compresión y encriptación si está habilitada usando propiedades individuales
        /// </summary>
        public static HttpContent CreateContent(
            object otlpPayload,
            bool enableCompression,
            EncryptionService? encryptionService = null,
            bool encryptInTransit = false,
            ILogger? logger = null)
        {
            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(otlpPayload, JsonOptions);

            // Encriptación en tránsito si está habilitada
            if (encryptInTransit && encryptionService != null)
            {
                jsonBytes = encryptionService.Encrypt(jsonBytes);
                logger?.LogDebug("OTLP payload encrypted for transit");
            }

            // Compresión si está habilitada y el payload es suficientemente grande
            if (enableCompression && jsonBytes.Length > 1024)
            {
                var compressed = Utils.CompressionHelper.CompressGZip(jsonBytes);
                var content = new ByteArrayContent(compressed);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                content.Headers.ContentEncoding.Add("gzip");
                if (encryptInTransit)
                {
                    content.Headers.Add("X-Encrypted", "true");
                }
                return content;
            }
            else
            {
                var content = new ByteArrayContent(jsonBytes);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                if (encryptInTransit)
                {
                    content.Headers.Add("X-Encrypted", "true");
                }
                return content;
            }
        }
    }
}

