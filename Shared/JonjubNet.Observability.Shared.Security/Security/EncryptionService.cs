using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Buffers;

namespace JonjubNet.Observability.Shared.Security
{
    /// <summary>
    /// Servicio de encriptación para datos en tránsito y reposo
    /// Común para Metrics y Logging
    /// </summary>
    public class EncryptionService
    {
        private readonly ILogger<EncryptionService>? _logger;
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public EncryptionService(ILogger<EncryptionService>? logger = null)
        {
            _logger = logger;
            // En producción, estas claves deben venir de configuración segura
            _key = GenerateKey();
            _iv = GenerateIV();
        }

        public EncryptionService(byte[] key, byte[] iv, ILogger<EncryptionService>? logger = null)
        {
            _logger = logger;
            _key = key;
            _iv = iv;
        }

        /// <summary>
        /// Encripta datos usando AES
        /// </summary>
        public byte[] Encrypt(byte[] data)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor();
                using var msEncrypt = new MemoryStream();
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    csEncrypt.Write(data, 0, data.Length);
                }

                // Optimizado: usar GetBuffer() si es posible para evitar copia
                if (msEncrypt.TryGetBuffer(out var buffer))
                {
                    var result = new byte[buffer.Count];
                    Buffer.BlockCopy(buffer.Array!, buffer.Offset, result, 0, buffer.Count);
                    return result;
                }
                
                return msEncrypt.ToArray();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error encrypting data");
                throw;
            }
        }

        /// <summary>
        /// Desencripta datos usando AES
        /// </summary>
        public byte[] Decrypt(byte[] encryptedData)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                using var msDecrypt = new MemoryStream(encryptedData);
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var msResult = new MemoryStream();

                csDecrypt.CopyTo(msResult);
                
                // Optimizado: usar GetBuffer() si es posible para evitar copia
                if (msResult.TryGetBuffer(out var buffer))
                {
                    var result = new byte[buffer.Count];
                    Buffer.BlockCopy(buffer.Array!, buffer.Offset, result, 0, buffer.Count);
                    return result;
                }
                
                return msResult.ToArray();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error decrypting data");
                throw;
            }
        }

        /// <summary>
        /// Encripta una cadena de texto
        /// </summary>
        public string EncryptString(string plainText)
        {
            var bytes = Encoding.UTF8.GetBytes(plainText);
            var encrypted = Encrypt(bytes);
            return Convert.ToBase64String(encrypted);
        }

        /// <summary>
        /// Desencripta una cadena de texto
        /// </summary>
        public string DecryptString(string encryptedText)
        {
            var bytes = Convert.FromBase64String(encryptedText);
            var decrypted = Decrypt(bytes);
            return Encoding.UTF8.GetString(decrypted);
        }

        private static byte[] GenerateKey()
        {
            using var aes = Aes.Create();
            aes.GenerateKey();
            return aes.Key;
        }

        private static byte[] GenerateIV()
        {
            using var aes = Aes.Create();
            aes.GenerateIV();
            return aes.IV;
        }
    }
}

