namespace JonjubNet.Observability.Shared.OpenTelemetry
{
    /// <summary>
    /// Protocolo OTLP (OpenTelemetry Protocol) soportado
    /// Compartido entre Metrics y Logging
    /// </summary>
    public enum OtlpProtocol
    {
        HttpProtobuf,
        HttpJson,
        Grpc
    }
}

