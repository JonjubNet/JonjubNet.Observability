namespace JonjubNet.Observability.Metrics.Shared.Security
{
    /// <summary>
    /// Opciones de seguridad TLS/SSL para adapters HTTP
    /// </summary>
    public class TlsOptions
    {
        /// <summary>
        /// Habilitar validación de certificados SSL
        /// </summary>
        public bool ValidateCertificates { get; set; } = true;

        /// <summary>
        /// Permitir certificados auto-firmados (solo para desarrollo)
        /// </summary>
        public bool AllowSelfSignedCertificates { get; set; } = false;

        /// <summary>
        /// Ruta al certificado cliente (opcional)
        /// </summary>
        public string? ClientCertificatePath { get; set; }

        /// <summary>
        /// Contraseña del certificado cliente (opcional)
        /// </summary>
        public string? ClientCertificatePassword { get; set; }

        /// <summary>
        /// Versión mínima de TLS permitida
        /// </summary>
        public System.Security.Authentication.SslProtocols MinTlsVersion { get; set; } = System.Security.Authentication.SslProtocols.Tls12;

        /// <summary>
        /// Habilitar encriptación en tránsito
        /// </summary>
        public bool EnableEncryption { get; set; } = true;
    }
}

