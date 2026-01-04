#if GRPC_SUPPORT
using Grpc.Core;
using Grpc.Core.Interceptors;
#endif
using JonjubNet.Observability.Shared.Context;
using JonjubNet.Observability.Shared.Context.Protocols;

namespace JonjubNet.Observability.Hosting.Grpc
{
#if GRPC_SUPPORT
    /// <summary>
    /// Interceptor de gRPC para propagación automática de CorrelationId en requests salientes
    /// Thread-safe, sin overhead innecesario, optimizado para performance
    /// Requiere: Paquete Grpc.Core (opcional)
    /// </summary>
    public class GrpcClientCorrelationInterceptor : Interceptor
    {
        /// <summary>
        /// Intercepta llamadas unary de gRPC para agregar CorrelationId en metadata
        /// </summary>
        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            // Agregar CorrelationId a metadata si está disponible
            var metadata = GrpcCorrelationHelper.CreateMetadata();
            if (metadata != null)
            {
                var options = context.Options;
                foreach (var kvp in metadata)
                {
                    options.Headers.Add(kvp.Key, kvp.Value);
                }
                context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
            }

            return continuation(request, context);
        }

        /// <summary>
        /// Intercepta llamadas streaming de gRPC para agregar CorrelationId en metadata
        /// </summary>
        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            var metadata = GrpcCorrelationHelper.CreateMetadata();
            if (metadata != null)
            {
                var options = context.Options;
                foreach (var kvp in metadata)
                {
                    options.Headers.Add(kvp.Key, kvp.Value);
                }
                context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
            }

            return continuation(context);
        }

        /// <summary>
        /// Intercepta llamadas server streaming de gRPC para agregar CorrelationId en metadata
        /// </summary>
        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            var metadata = GrpcCorrelationHelper.CreateMetadata();
            if (metadata != null)
            {
                var options = context.Options;
                foreach (var kvp in metadata)
                {
                    options.Headers.Add(kvp.Key, kvp.Value);
                }
                context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
            }

            return continuation(request, context);
        }

        /// <summary>
        /// Intercepta llamadas bidirectional streaming de gRPC para agregar CorrelationId en metadata
        /// </summary>
        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            var metadata = GrpcCorrelationHelper.CreateMetadata();
            if (metadata != null)
            {
                var options = context.Options;
                foreach (var kvp in metadata)
                {
                    options.Headers.Add(kvp.Key, kvp.Value);
                }
                context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
            }

            return continuation(context);
        }
    }

    /// <summary>
    /// Interceptor de gRPC para lectura automática de CorrelationId en requests entrantes
    /// Thread-safe, sin overhead innecesario, optimizado para performance
    /// Requiere: Paquete Grpc.Core (opcional)
    /// </summary>
    public class GrpcServerCorrelationInterceptor : Interceptor
    {
        /// <summary>
        /// Intercepta llamadas unary de gRPC para leer CorrelationId de metadata
        /// </summary>
        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            // Extraer CorrelationId de metadata y establecer en contexto
            var correlationId = ExtractCorrelationIdFromContext(context);
            if (!string.IsNullOrEmpty(correlationId))
            {
                ObservabilityContext.SetCorrelationId(correlationId);
            }
            else
            {
                // Si no se recibe, generar uno nuevo (mejores prácticas)
                var newCorrelationId = TraceIdGenerator.GenerateCorrelationId();
                ObservabilityContext.SetCorrelationId(newCorrelationId);
            }

            try
            {
                return await continuation(request, context);
            }
            finally
            {
                // Limpiar contexto al finalizar
                ObservabilityContext.Clear();
            }
        }

        /// <summary>
        /// Intercepta llamadas streaming de gRPC para leer CorrelationId de metadata
        /// </summary>
        public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
            TRequest request,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            var correlationId = ExtractCorrelationIdFromContext(context);
            if (!string.IsNullOrEmpty(correlationId))
            {
                ObservabilityContext.SetCorrelationId(correlationId);
            }
            else
            {
                var newCorrelationId = TraceIdGenerator.GenerateCorrelationId();
                ObservabilityContext.SetCorrelationId(newCorrelationId);
            }

            try
            {
                await continuation(request, responseStream, context);
            }
            finally
            {
                ObservabilityContext.Clear();
            }
        }

        /// <summary>
        /// Intercepta llamadas client streaming de gRPC para leer CorrelationId de metadata
        /// </summary>
        public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream,
            ServerCallContext context,
            ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            var correlationId = ExtractCorrelationIdFromContext(context);
            if (!string.IsNullOrEmpty(correlationId))
            {
                ObservabilityContext.SetCorrelationId(correlationId);
            }
            else
            {
                var newCorrelationId = TraceIdGenerator.GenerateCorrelationId();
                ObservabilityContext.SetCorrelationId(newCorrelationId);
            }

            try
            {
                return await continuation(requestStream, context);
            }
            finally
            {
                ObservabilityContext.Clear();
            }
        }

        /// <summary>
        /// Intercepta llamadas bidirectional streaming de gRPC para leer CorrelationId de metadata
        /// </summary>
        public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            DuplexStreamingServerMethod<TRequest, TResponse> continuation)
        {
            var correlationId = ExtractCorrelationIdFromContext(context);
            if (!string.IsNullOrEmpty(correlationId))
            {
                ObservabilityContext.SetCorrelationId(correlationId);
            }
            else
            {
                var newCorrelationId = TraceIdGenerator.GenerateCorrelationId();
                ObservabilityContext.SetCorrelationId(newCorrelationId);
            }

            try
            {
                await continuation(requestStream, responseStream, context);
            }
            finally
            {
                ObservabilityContext.Clear();
            }
        }

        /// <summary>
        /// Extrae CorrelationId de metadata de gRPC
        /// Optimizado: búsqueda directa sin LINQ
        /// </summary>
        private string? ExtractCorrelationIdFromContext(ServerCallContext context)
        {
            var headerName = CorrelationPropagationHelper.CorrelationIdHeaderName;

            // Búsqueda directa (optimización: evitar LINQ)
            foreach (var entry in context.RequestHeaders)
            {
                if (entry.Key == headerName)
                {
                    return entry.Value;
                }
            }

            // Búsqueda case-insensitive (optimización: solo si no se encontró)
            foreach (var entry in context.RequestHeaders)
            {
                if (string.Equals(entry.Key, headerName, StringComparison.OrdinalIgnoreCase))
                {
                    return entry.Value;
                }
            }

            return null;
        }
    }
#else
    // gRPC support requiere el paquete Grpc.Core
    // Para habilitar, agregue la referencia al paquete y defina GRPC_SUPPORT en el proyecto
    // Ejemplo: <DefineConstants>$(DefineConstants);GRPC_SUPPORT</DefineConstants>
#endif
}

