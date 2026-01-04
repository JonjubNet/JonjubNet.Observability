using JonjubNet.Observability.Metrics.Core.Interfaces;
using JonjubNet.Observability.Metrics.Core.Utils;
using System.Collections.Concurrent;
using System.Data.Common;

namespace JonjubNet.Observability.Metrics.Shared.Database
{
    /// <summary>
    /// Helper optimizado para capturar métricas de base de datos.
    /// Thread-safe, sin allocations innecesarias, sin estado persistente, sin memory leaks.
    /// Parte del componente base - puede ser usado por cualquier microservicio.
    /// </summary>
    public static class DatabaseMetricsHelper
    {
        private static readonly string OperationRead = string.Intern("read");
        private static readonly string OperationWrite = string.Intern("write");
        private static readonly string OperationUnknown = string.Intern("unknown");
        private static readonly string TenantUnknown = string.Intern("unknown");
        private static readonly string DatabaseTypeRead = string.Intern("read");
        private static readonly string DatabaseTypeWrite = string.Intern("write");
        private static readonly string StatusSuccess = string.Intern("success");
        private static readonly string StatusError = string.Intern("error");

        // Cache limitado de tenant codes para evitar memory leaks con string.Intern
        // Usa ConcurrentDictionary para thread-safety sin locks
        private static readonly ConcurrentDictionary<string, string> _tenantCache = new();
        private static readonly ConcurrentDictionary<string, string> _databaseTypeCache = new();

        /// <summary>
        /// Captura métricas de operación de base de datos de forma optimizada.
        /// </summary>
        public static void RecordDatabaseOperation(
            IMetricsClient? metrics,
            Func<string?>? getTenantCode,
            string operation,
            string databaseType,
            long durationMs,
            bool success = true,
            string? metricPrefix = null)
        {
            if (metrics == null) return;

            try
            {
                var tenantCode = getTenantCode?.Invoke() ?? TenantUnknown;
                var internedTenant = GetOrCacheTenant(tenantCode);
                
                // Operations son valores conocidos, usar referencias directas
                var internedOperation = ReferenceEquals(operation, OperationRead) ? OperationRead :
                                       ReferenceEquals(operation, OperationWrite) ? OperationWrite :
                                       OperationUnknown;
                var internedDbType = GetOrCacheDatabaseType(databaseType);
                var internedStatus = success ? StatusSuccess : StatusError;

                var prefix = string.IsNullOrEmpty(metricPrefix) ? "database" : metricPrefix;
                
                // OPTIMIZACIÓN: Usar CollectionPool para reutilizar diccionarios y reducir allocations
                var labels = CollectionPool.RentDictionary();
                try
                {
                    labels["operation"] = internedOperation;
                    labels["database_type"] = internedDbType;
                    labels["tenant"] = internedTenant;
                    labels["status"] = internedStatus;

                var durationSeconds = durationMs / 1000.0;
                metrics.RecordHistogram($"{prefix}_operation_duration_seconds", durationSeconds, labels);
                metrics.Increment($"{prefix}_operations_total", 1.0, labels);

                if (!success)
                {
                    metrics.Increment($"{prefix}_operations_errors_total", 1.0, labels);
                    }
                }
                finally
                {
                    // Devolver el diccionario al pool para reutilización
                    CollectionPool.ReturnDictionary(labels);
                }
            }
            catch
            {
                // Silenciar errores de métricas - no deben afectar la operación
            }
        }

        /// <summary>
        /// Captura métricas de SaveChanges optimizado.
        /// </summary>
        public static void RecordSaveChanges(
            IMetricsClient? metrics,
            Func<string?>? getTenantCode,
            string databaseType,
            long durationMs,
            int entitiesAffected,
            bool success = true,
            string? metricPrefix = null)
        {
            if (metrics == null) return;

            try
            {
                var tenantCode = getTenantCode?.Invoke() ?? TenantUnknown;
                var internedTenant = GetOrCacheTenant(tenantCode);
                var internedDbType = GetOrCacheDatabaseType(databaseType);
                var internedStatus = success ? StatusSuccess : StatusError;

                var prefix = string.IsNullOrEmpty(metricPrefix) ? "database" : metricPrefix;
                
                // OPTIMIZACIÓN: Usar CollectionPool para reutilizar diccionarios y reducir allocations
                var labels = CollectionPool.RentDictionary();
                try
                {
                    labels["database_type"] = internedDbType;
                    labels["tenant"] = internedTenant;
                    labels["status"] = internedStatus;

                var durationSeconds = durationMs / 1000.0;
                metrics.RecordHistogram($"{prefix}_savechanges_duration_seconds", durationSeconds, labels);
                metrics.RecordHistogram($"{prefix}_savechanges_entities_affected", entitiesAffected, labels);
                metrics.Increment($"{prefix}_savechanges_total", 1.0, labels);

                if (!success)
                {
                    metrics.Increment($"{prefix}_savechanges_errors_total", 1.0, labels);
                    }
                }
                finally
                {
                    // Devolver el diccionario al pool para reutilización
                    CollectionPool.ReturnDictionary(labels);
                }
            }
            catch
            {
                // Silenciar errores de métricas
            }
        }

        /// <summary>
        /// Obtiene el tipo de operación desde el nombre del método.
        /// </summary>
        public static string GetOperationType(string methodName, string databaseType)
        {
            if (methodName.Contains("Get", StringComparison.OrdinalIgnoreCase) ||
                methodName.Contains("Find", StringComparison.OrdinalIgnoreCase) ||
                methodName.Contains("List", StringComparison.OrdinalIgnoreCase) ||
                methodName.Contains("Count", StringComparison.OrdinalIgnoreCase) ||
                methodName.Contains("Any", StringComparison.OrdinalIgnoreCase) ||
                methodName.Contains("First", StringComparison.OrdinalIgnoreCase) ||
                methodName.Contains("Single", StringComparison.OrdinalIgnoreCase))
            {
                return OperationRead;
            }

            if (methodName.Contains("Add", StringComparison.OrdinalIgnoreCase) ||
                methodName.Contains("Update", StringComparison.OrdinalIgnoreCase) ||
                methodName.Contains("Delete", StringComparison.OrdinalIgnoreCase) ||
                methodName.Contains("Save", StringComparison.OrdinalIgnoreCase))
            {
                return OperationWrite;
            }

            return OperationUnknown;
        }

        private static string GetOrCacheTenant(string tenantCode)
        {
            if (ReferenceEquals(tenantCode, TenantUnknown))
                return TenantUnknown;

            // Usar cache limitado para evitar memory leaks
            if (_tenantCache.Count > 1000)
                return tenantCode;

            return _tenantCache.GetOrAdd(tenantCode, key => string.Intern(key));
        }

        private static string GetOrCacheDatabaseType(string databaseType)
        {
            if (ReferenceEquals(databaseType, DatabaseTypeRead))
                return DatabaseTypeRead;
            if (ReferenceEquals(databaseType, DatabaseTypeWrite))
                return DatabaseTypeWrite;

            if (_databaseTypeCache.Count > 50)
                return databaseType;

            return _databaseTypeCache.GetOrAdd(databaseType, key => string.Intern(key));
        }

        /// <summary>
        /// Detecta el tipo de base de datos desde la conexión (agnóstico de tecnología).
        /// Funciona con SQL Server, MySQL, PostgreSQL, Oracle y cualquier proveedor.
        /// </summary>
        public static string DetectDatabaseType(DbConnection? connection)
        {
            if (connection == null)
                return "unknown";

            var connectionTypeName = connection.GetType().Name.ToLowerInvariant();
            
            if (connectionTypeName.Contains("sqlconnection"))
                return "sqlserver";
            if (connectionTypeName.Contains("mysqlconnection") || connectionTypeName.Contains("mariadbconnection"))
                return "mysql";
            if (connectionTypeName.Contains("npgsqlconnection"))
                return "postgresql";
            if (connectionTypeName.Contains("oracleconnection"))
                return "oracle";

            return "unknown";
        }
    }
}

