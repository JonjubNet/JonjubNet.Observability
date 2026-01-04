using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Observability.Shared.Security
{
    /// <summary>
    /// Factory para crear HttpClient seguros con TLS/SSL
    /// Común para Metrics y Logging
    /// </summary>
    public class SecureHttpClientFactory
    {
        private readonly bool _validateCertificates;
        private readonly X509Certificate2? _clientCertificate;
        private readonly ILogger? _logger;

        public SecureHttpClientFactory(
            bool validateCertificates = true,
            X509Certificate2? clientCertificate = null,
            ILogger? logger = null)
        {
            _validateCertificates = validateCertificates;
            _clientCertificate = clientCertificate;
            _logger = logger;
        }

        /// <summary>
        /// Crea un HttpClient seguro con TLS/SSL habilitado
        /// </summary>
        public HttpClient CreateSecureClient(string baseUrl)
        {
            var handler = new HttpClientHandler();

            // Configurar certificado de cliente si está disponible
            if (_clientCertificate != null)
            {
                handler.ClientCertificates.Add(_clientCertificate);
            }

            // Configurar validación de certificados
            if (!_validateCertificates)
            {
                // Solo para desarrollo/testing - NO usar en producción
                handler.ServerCertificateCustomValidationCallback = 
                    (message, cert, chain, errors) => true;
                
                _logger?.LogWarning("Certificate validation is disabled. This should only be used in development/testing.");
            }
            else
            {
                // Validación personalizada de certificados
                handler.ServerCertificateCustomValidationCallback = 
                    ValidateServerCertificate;
            }

            var client = new HttpClient(handler);
            
            if (!string.IsNullOrEmpty(baseUrl))
            {
                client.BaseAddress = new Uri(baseUrl);
            }

            return client;
        }

        /// <summary>
        /// Valida el certificado del servidor
        /// </summary>
        private bool ValidateServerCertificate(
            HttpRequestMessage message,
            X509Certificate2? certificate,
            X509Chain? chain,
            SslPolicyErrors sslPolicyErrors)
        {
            // Si no hay errores, aceptar
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            // Log de errores
            _logger?.LogWarning("SSL certificate validation error: {Errors}", sslPolicyErrors);

            // Rechazar si hay errores críticos
            if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable) ||
                sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
            {
                return false;
            }

            // Para otros errores, validar la cadena
            if (chain != null && certificate != null)
            {
                var chainStatus = chain.ChainStatus;
                foreach (var status in chainStatus)
                {
                    // Permitir algunos errores comunes en desarrollo
                    if (status.Status == X509ChainStatusFlags.UntrustedRoot ||
                        status.Status == X509ChainStatusFlags.PartialChain)
                    {
                        _logger?.LogWarning("Certificate chain issue: {Status}", status.Status);
                        // En producción, esto debería ser false
                        // En desarrollo, puede ser true si se acepta
                        return false; // Por defecto, rechazar
                    }
                }
            }

            return false;
        }
    }
}

