namespace JonjubNet.Observability.Shared.OpenTelemetry
{
    /// <summary>
    /// Builder para construir URLs OTLP
    /// Compartido entre Metrics y Logging
    /// </summary>
    public static class OtlpUrlBuilder
    {
        /// <summary>
        /// Construye la URL para el endpoint de métricas usando OtlpOptions
        /// </summary>
        public static string BuildMetricsUrl(OtlpOptions options)
        {
            return BuildUrl(options.Endpoint, options.Protocol, "metrics");
        }

        /// <summary>
        /// Construye la URL para el endpoint de logs usando OtlpOptions
        /// </summary>
        public static string BuildLogsUrl(OtlpOptions options)
        {
            return BuildUrl(options.Endpoint, options.Protocol, "logs");
        }

        /// <summary>
        /// Construye la URL para el endpoint de traces usando OtlpOptions (futuro)
        /// </summary>
        public static string BuildTracesUrl(OtlpOptions options)
        {
            return BuildUrl(options.Endpoint, options.Protocol, "traces");
        }

        /// <summary>
        /// Construye la URL para el endpoint usando propiedades individuales
        /// </summary>
        public static string BuildUrl(string endpoint, OtlpProtocol protocol, string resourceType)
        {
            return protocol switch
            {
                OtlpProtocol.HttpProtobuf => $"{endpoint}/v1/{resourceType}",
                OtlpProtocol.HttpJson => $"{endpoint}/v1/{resourceType}",
                OtlpProtocol.Grpc => throw new NotSupportedException("gRPC protocol requires additional libraries"),
                _ => $"{endpoint}/v1/{resourceType}"
            };
        }
    }
}

