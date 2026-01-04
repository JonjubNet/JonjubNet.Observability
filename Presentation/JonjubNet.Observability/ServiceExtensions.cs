using JonjubNet.Observability.Hosting;
using JonjubNet.Observability.Metrics.Shared.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JonjubNet.Observability
{
    /// <summary>
    /// Extensiones para configurar la infraestructura de métricas
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// Agrega la infraestructura de métricas al contenedor de dependencias
        /// </summary>
        /// <param name="services">Colección de servicios</param>
        /// <param name="configuration">Configuración de la aplicación</param>
        /// <returns>Colección de servicios para chaining</returns>
        public static IServiceCollection AddMetricsInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddJonjubNetMetrics(configuration);
        }

        /// <summary>
        /// Agrega la infraestructura de métricas con configuración personalizada
        /// </summary>
        /// <param name="services">Colección de servicios</param>
        /// <param name="configuration">Configuración de la aplicación</param>
        /// <param name="configureOptions">Acción para configurar opciones adicionales</param>
        /// <returns>Colección de servicios para chaining</returns>
        public static IServiceCollection AddMetricsInfrastructure(
            this IServiceCollection services, 
            IConfiguration configuration, 
            Action<MetricsOptions> configureOptions)
        {
            return services.AddJonjubNetMetrics(configuration, configureOptions);
        }

        /// <summary>
        /// Agrega middleware de métricas HTTP al pipeline de ASP.NET Core
        /// </summary>
        /// <param name="app">Builder de la aplicación</param>
        /// <returns>Builder de la aplicación para chaining</returns>
        public static IApplicationBuilder UseMetricsMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<Hosting.MetricsHttpMiddlewareExporter>();
        }
    }
}
