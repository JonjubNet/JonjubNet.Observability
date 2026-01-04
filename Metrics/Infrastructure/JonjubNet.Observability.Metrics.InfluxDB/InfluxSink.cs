using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.IO.Compression;
using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.Core.Interfaces;
using JonjubNet.Observability.Metrics.Core.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using JonjubNet.Observability.Shared.Security;

namespace JonjubNet.Observability.Metrics.InfluxDB
{
    /// <summary>
    /// Sink de métricas para InfluxDB
    /// </summary>
    public class InfluxSink : IMetricsSink
    {
        private readonly InfluxOptions _options;
        private readonly ILogger<InfluxSink>? _logger;
        private readonly HttpClient? _httpClient;
        private readonly EncryptionService? _encryptionService;
        private readonly bool _encryptInTransit;

        public string Name => "InfluxDB";
        public bool IsEnabled => _options.Enabled;

        public InfluxSink(
            IOptions<InfluxOptions> options,
            ILogger<InfluxSink>? logger = null,
            HttpClient? httpClient = null,
            EncryptionService? encryptionService = null,
            SecureHttpClientFactory? secureHttpClientFactory = null,
            bool encryptInTransit = false,
            bool enableTls = true)
        {
            _options = options.Value;
            _logger = logger;
            _encryptionService = encryptionService;
            _encryptInTransit = encryptInTransit;
            
            // Solo crear HttpClient si el sink está habilitado
            // Esto evita crear recursos innecesarios si está deshabilitado
            if (_options.Enabled)
            {
                // Usar SecureHttpClientFactory si TLS está habilitado y está disponible
                if (enableTls && secureHttpClientFactory != null && !string.IsNullOrEmpty(_options.Url))
                {
                    _httpClient = secureHttpClientFactory.CreateSecureClient(_options.Url);
                }
                else
                {
                    _httpClient = httpClient ?? new HttpClient();
                    if (!string.IsNullOrEmpty(_options.Url))
                    {
                    _httpClient!.BaseAddress = new Uri(_options.Url);
                }
            }

            if (!string.IsNullOrEmpty(_options.Token))
            {
                _httpClient!.DefaultRequestHeaders.Add("Authorization", $"Token {_options.Token}");
                }
            }
            else
            {
                // Si está deshabilitado, usar un HttpClient nulo o el proporcionado (no crear uno nuevo)
                _httpClient = httpClient;
            }
        }

        /// <summary>
        /// Exporta métricas desde el Registry (método principal optimizado)
        /// </summary>
        public async ValueTask ExportFromRegistryAsync(MetricRegistry registry, CancellationToken cancellationToken = default)
        {
            // Verificar si está habilitado - si no, retornar inmediatamente sin hacer nada
            if (!_options.Enabled)
                return;
            
            // Verificar que HttpClient esté disponible (debería estar si Enabled = true)
            if (_httpClient == null)
            {
                _logger?.LogWarning("InfluxDB is enabled but HttpClient is not available. Skipping export.");
                return;
            }

            try
            {
                var lineProtocol = FormatRegistryAsLineProtocol(registry);
                if (string.IsNullOrEmpty(lineProtocol))
                    return;

                var url = $"{_options.Url}/api/v2/write?org={Uri.EscapeDataString(_options.Organization ?? "default")}&bucket={Uri.EscapeDataString(_options.Bucket)}";
                
                var lineProtocolBytes = Encoding.UTF8.GetBytes(lineProtocol);
                
                // Encriptación en tránsito si está habilitada
                if (_encryptInTransit && _encryptionService != null)
                {
                    lineProtocolBytes = _encryptionService.Encrypt(lineProtocolBytes);
                    _logger?.LogDebug("Metrics encrypted for transit to InfluxDB");
                }
                
                HttpContent content;

                if (_options.EnableCompression && lineProtocolBytes.Length > 1024)
                {
                    using var ms = new MemoryStream();
                    using (var gzip = new GZipStream(ms, CompressionLevel.Fastest))
                    {
                        await gzip.WriteAsync(lineProtocolBytes, cancellationToken);
                    }
                    var compressed = ms.ToArray();
                    content = new ByteArrayContent(compressed);
                    content.Headers.ContentEncoding.Add("gzip");
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
                    if (_encryptInTransit)
                    {
                        content.Headers.Add("X-Encrypted", "true");
                    }
                }
                else
                {
                    content = new ByteArrayContent(lineProtocolBytes);
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
                    if (_encryptInTransit)
                    {
                        content.Headers.Add("X-Encrypted", "true");
                    }
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
                }

                var response = await _httpClient!.PostAsync(url, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger?.LogWarning("InfluxDB export failed with status {StatusCode}: {Error}", 
                        response.StatusCode, errorContent);
                }
                else
                {
                    _logger?.LogDebug("Exported metrics to InfluxDB");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exporting metrics to InfluxDB");
            }
        }

        private string FormatRegistryAsLineProtocol(MetricRegistry registry)
        {
            var sb = new StringBuilder(4096); // Pre-allocate capacity
            var timestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();

            // Convertir Counters
            foreach (var counter in registry.GetAllCounters().Values)
            {
                foreach (var (key, value) in counter.GetAllValues())
                {
                    var tags = ParseKey(key);
                    sb.Append(SanitizeMeasurement(counter.Name));
                    
                    if (tags.Count > 0)
                    {
                        foreach (var kvp in tags)
                        {
                            sb.Append(',').Append(SanitizeTag(kvp.Key)).Append('=').Append(SanitizeTag(kvp.Value));
                        }
                    }
                    
                    sb.Append(" value=").Append(value).Append(' ').Append(timestamp).Append('\n');
                }
            }

            // Convertir Gauges
            foreach (var gauge in registry.GetAllGauges().Values)
            {
                foreach (var (key, value) in gauge.GetAllValues())
                {
                    var tags = ParseKey(key);
                    sb.Append(SanitizeMeasurement(gauge.Name));
                    
                    if (tags.Count > 0)
                    {
                        foreach (var kvp in tags)
                        {
                            sb.Append(',').Append(SanitizeTag(kvp.Key)).Append('=').Append(SanitizeTag(kvp.Value));
                        }
                    }
                    
                    sb.Append(" value=").Append(value).Append(' ').Append(timestamp).Append('\n');
                }
            }

            // Similar para Histograms...

            // Remove trailing newline
            if (sb.Length > 0 && sb[sb.Length - 1] == '\n')
            {
                sb.Length--;
            }
            
            return sb.ToString();
        }

        private Dictionary<string, string> ParseKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return new Dictionary<string, string>();

            var result = new Dictionary<string, string>();
            var pairs = key.Split(',');
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=');
                if (parts.Length == 2)
                {
                    result[parts[0]] = parts[1];
                }
            }
            return result;
        }


        private static string SanitizeMeasurement(string value)
        {
            return value.Replace(",", "\\,").Replace(" ", "\\ ");
        }

        private static string SanitizeTag(string value)
        {
            return value.Replace(",", "\\,").Replace("=", "\\=").Replace(" ", "\\ ");
        }
    }
}
