using JonjubNet.Observability.Shared.Security;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Shared.OpenTelemetry
{
    /// <summary>
    /// Helper para crear y configurar HttpClient para OTLP
    /// Compartido entre Metrics y Logging
    /// </summary>
    public static class OtlpHttpClientHelper
    {
        /// <summary>
        /// Crea un HttpClient configurado para OTLP usando OtlpOptions
        /// </summary>
        public static HttpClient? CreateHttpClient(
            OtlpOptions options,
            SecureHttpClientFactory? secureHttpClientFactory = null,
            HttpClient? providedHttpClient = null,
            bool enableTls = true,
            ILogger? logger = null)
        {
            return CreateHttpClient(
                options.Enabled,
                options.Endpoint,
                options.TimeoutSeconds,
                secureHttpClientFactory,
                providedHttpClient,
                enableTls,
                logger);
        }

        /// <summary>
        /// Crea un HttpClient configurado para OTLP usando propiedades individuales
        /// </summary>
        public static HttpClient? CreateHttpClient(
            bool enabled,
            string endpoint,
            int timeoutSeconds,
            SecureHttpClientFactory? secureHttpClientFactory = null,
            HttpClient? providedHttpClient = null,
            bool enableTls = true,
            ILogger? logger = null)
        {
            // Si está deshabilitado, retornar null o el proporcionado
            if (!enabled)
            {
                return providedHttpClient;
            }

            HttpClient? httpClient;

            // Usar SecureHttpClientFactory si TLS está habilitado y está disponible
            if (enableTls && secureHttpClientFactory != null && !string.IsNullOrEmpty(endpoint))
            {
                httpClient = secureHttpClientFactory.CreateSecureClient(endpoint);
            }
            else
            {
                httpClient = providedHttpClient ?? new HttpClient();
            }

            // Configurar timeout
            if (httpClient != null)
            {
                httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            }

            return httpClient;
        }
    }
}

