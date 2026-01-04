namespace JonjubNet.Observability.Shared.Context
{
    /// <summary>
    /// Interfaz para propagación de CorrelationId en diferentes protocolos
    /// Abstracción común para evitar duplicación de código
    /// </summary>
    public interface ICorrelationPropagator
    {
        /// <summary>
        /// Propaga CorrelationId al protocolo específico
        /// </summary>
        void PropagateCorrelationId(string correlationId);

        /// <summary>
        /// Extrae CorrelationId del protocolo específico
        /// </summary>
        string? ExtractCorrelationId();
    }
}

