# Guía de Implementación Unificada - Logging, Metrics y Tracing

> **Versión:** 1.0.0 | **Última actualización:** Diciembre 2024  
> **Componente:** JonjubNet.Observability  
> **Nivel:** Producción Enterprise

---

## 📋 Tabla de Contenidos

1. [Introducción](#introducción)
2. [Instalación y Configuración Inicial](#instalación-y-configuración-inicial)
3. [Logging](#logging)
4. [Metrics](#metrics)
5. [Tracing](#tracing)
6. [Correlación y Trazabilidad](#correlación-y-trazabilidad)
7. [Configuración Avanzada](#configuración-avanzada)
8. [Mejores Prácticas](#mejores-prácticas)
9. [Troubleshooting](#troubleshooting)
10. [Recursos Adicionales](#recursos-adicionales)

---

## Introducción

**JonjubNet.Observability** es una biblioteca de observabilidad de nivel empresarial que proporciona **Logging**, **Metrics** y **Tracing** distribuido en un solo componente unificado. Esta guía te llevará paso a paso a través de la implementación completa de las tres funcionalidades.

### 🎯 Objetivo de esta Guía

Esta guía está diseñada para:
- ✅ Desarrolladores que implementan observabilidad por primera vez
- ✅ Equipos que migran de otras soluciones
- ✅ Arquitectos que evalúan el componente
- ✅ DevOps que configuran la infraestructura

### ✨ Características Principales

| Característica | Descripción | Beneficio |
|----------------|-------------|-----------|
| **Unificado** | Logging, Metrics y Tracing en un solo componente | Simplifica integración y mantenimiento |
| **Correlación Automática** | CorrelationId propagado automáticamente | Trazabilidad completa de transacciones |
| **Múltiples Destinos** | Soporte para múltiples sinks/exporters simultáneos | Flexibilidad en infraestructura |
| **Resiliencia** | Dead Letter Queue, Retry Policy, Circuit Breaker | Alta disponibilidad y confiabilidad |
| **Configuración Dinámica** | Hot-reload sin reiniciar la aplicación | Operaciones sin downtime |
| **Performance Optimizado** | Thread-safe, sin overhead innecesario, optimización GC | Bajo impacto en aplicaciones |

### 📊 Capacidades del Componente

#### **Logging**
- ✅ 6 Sinks: Console, Serilog, Kafka, HTTP, Elasticsearch, OpenTelemetry
- ✅ Logging estructurado completo
- ✅ Filtrado avanzado
- ✅ Enriquecimiento automático

#### **Metrics**
- ✅ 7 Sinks: Prometheus, InfluxDB, StatsD, Kafka, OpenTelemetry, Elasticsearch, HTTP
- ✅ 4 Tipos: Counters, Gauges, Histograms, Summaries
- ✅ Agregación avanzada
- ✅ Tags dinámicos

#### **Tracing**
- ✅ 4 Exporters: OpenTelemetry, Kafka, Elasticsearch, HTTP
- ✅ Spans completos
- ✅ Correlación automática
- ✅ Eventos y excepciones

#### **Correlación** 🆕 **v1.0.0**
- ✅ Propagación automática en todos los protocolos
- ✅ HTTP/REST, Kafka, gRPC, RabbitMQ, Azure Service Bus, SignalR
- ✅ Enriquecimiento automático en Logs, Metrics y Traces

---

## Instalación y Configuración Inicial

### 📦 Paso 1: Instalar el Paquete NuGet

#### Opción A: NuGet Package Manager
```powershell
Install-Package JonjubNet.Observability -Version 1.0.0
```

#### Opción B: .NET CLI
```bash
dotnet add package JonjubNet.Observability --version 1.0.0
```

#### Opción C: PackageReference
```xml
<ItemGroup>
  <PackageReference Include="JonjubNet.Observability" Version="1.0.0" />
</ItemGroup>
```

### ⚙️ Paso 2: Configurar en `Program.cs`

```csharp
using JonjubNet.Observability.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Registrar servicios de observabilidad
builder.Services.AddJonjubNetLogging(builder.Configuration);
builder.Services.AddJonjubNetMetrics(builder.Configuration);
builder.Services.AddJonjubNetTracing(builder.Configuration);

// Registrar correlación y middleware HTTP (v1.0.0)
builder.Services.AddJonjubNetObservability(builder.Configuration);

var app = builder.Build();

// Usar middleware de observabilidad (correlación automática)
app.UseJonjubNetObservability();

app.Run();
```

**Nota:** El middleware `UseJonjubNetObservability()` es opcional pero recomendado. Proporciona:
- ✅ Generación automática de CorrelationId
- ✅ Propagación automática en headers HTTP
- ✅ Logging automático de requests (opcional)
- ✅ Métricas automáticas de requests (opcional)
- ✅ Tracing automático de requests (opcional)

### 📝 Paso 3: Configurar en `appsettings.json`

```json
{
  "JonjubNet": {
    "Logging": {
      "Enabled": true,
      "DefaultLevel": "Information",
      "Sinks": {
        "Console": {
          "Enabled": true,
          "Format": "Json"
        },
        "Elasticsearch": {
          "Enabled": true,
          "BaseUrl": "http://localhost:9200",
          "IndexName": "logs",
          "Authentication": {
            "Type": "Basic",
            "Username": "elastic",
            "Password": "changeme"
          }
        }
      }
    },
    "Metrics": {
      "Enabled": true,
      "Sinks": {
        "Prometheus": {
          "Enabled": true,
          "Endpoint": "/metrics"
        },
        "OpenTelemetry": {
          "Enabled": true,
          "Endpoint": "http://localhost:4318",
          "Protocol": "HttpJson"
        }
      }
    },
    "Tracing": {
      "Enabled": true,
      "Exporters": {
        "OpenTelemetry": {
          "Enabled": true,
          "Endpoint": "http://localhost:4318",
          "Protocol": "HttpJson"
        }
      }
    },
    "Observability": {
      "Correlation": {
        "ReadIncomingCorrelationId": true,
        "CorrelationIdHeaderName": "X-Correlation-Id"
      },
      "HttpMiddleware": {
        "Enabled": true,
        "EnableAutomaticLogging": true,
        "EnableAutomaticMetrics": true,
        "EnableAutomaticTracing": true,
        "SampleRate": 1.0
      }
    }
  }
}
```

### ✅ Verificación de Instalación

Para verificar que la instalación fue exitosa, ejecuta tu aplicación y verifica:

1. **Logs en consola** (si Console sink está habilitado)
2. **Endpoint de métricas** (si Prometheus está habilitado): `http://localhost:5000/metrics`
3. **Headers de correlación** en requests HTTP (X-Correlation-Id)

---

## Logging

### 📚 Conceptos Básicos

**Logging** es el registro de eventos y mensajes que ocurren durante la ejecución de tu aplicación. JonjubNet.Observability proporciona logging estructurado con soporte para múltiples sinks.

### 🔧 Inyección de Dependencias

```csharp
public class OrderService
{
    private readonly ILoggingClient _logging;

    public OrderService(ILoggingClient logging)
    {
        _logging = logging;
    }

    // Tu código aquí...
}
```

### 📊 Niveles de Log

| Nivel | Uso | Ejemplo |
|-------|-----|---------|
| **Trace** | Información muy detallada (solo desarrollo) | `_logging.LogTrace("Variable value: {Value}", value);` |
| **Debug** | Información de depuración | `_logging.LogDebug("Processing order {OrderId}", orderId);` |
| **Information** | Información general | `_logging.LogInformation("Order {OrderId} created", orderId);` |
| **Warning** | Advertencias | `_logging.LogWarning("Rate limit approaching: {Current}/{Max}", current, max);` |
| **Error** | Errores | `_logging.LogError(exception, "Failed to process order {OrderId}", orderId);` |
| **Critical** | Errores críticos | `_logging.LogCritical(exception, "System failure: {Message}", message);` |

### 💡 Ejemplos Prácticos

#### Ejemplo 1: Logging Básico
```csharp
public async Task<Order> CreateOrderAsync(OrderRequest request)
{
    _logging.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);
    
    try
    {
        var order = await ProcessOrderAsync(request);
        _logging.LogInformation("Order {OrderId} created successfully", order.Id);
        return order;
    }
    catch (Exception ex)
    {
        _logging.LogError(ex, "Failed to create order for customer {CustomerId}", request.CustomerId);
        throw;
    }
}
```

#### Ejemplo 2: Logging Estructurado con Propiedades
```csharp
_logging.Log(LogLevel.Information, 
    "Order processed",
    properties: new Dictionary<string, object?>
    {
        { "OrderId", order.Id },
        { "CustomerId", order.CustomerId },
        { "Amount", order.Amount },
        { "Currency", order.Currency },
        { "Status", order.Status }
    });
```

#### Ejemplo 3: Logging con Tags
```csharp
_logging.Log(LogLevel.Information,
    "Order processed",
    tags: new Dictionary<string, string>
    {
        { "environment", "production" },
        { "region", "us-east-1" },
        { "service", "order-service" },
        { "version", "1.0.0" }
    });
```

### 🔄 Scopes y Operaciones

#### Scope para Agrupar Logs Relacionados
```csharp
using (_logging.BeginScope("ProcessingOrder", new Dictionary<string, object?>
{
    { "OrderId", orderId },
    { "CustomerId", customerId }
}))
{
    _logging.LogInformation("Step 1: Validating order");
    await ValidateOrderAsync(orderId);
    
    _logging.LogInformation("Step 2: Processing payment");
    await ProcessPaymentAsync(orderId);
    
    _logging.LogInformation("Step 3: Fulfilling order");
    await FulfillOrderAsync(orderId);
}
```

#### Operación con Duración Automática
```csharp
using (_logging.BeginOperation("ProcessOrder", new Dictionary<string, object?>
{
    { "OrderId", orderId }
}))
{
    // Tu lógica aquí
    // La duración se registra automáticamente al finalizar
    await ProcessOrderAsync(orderId);
}
```

### 🎯 Sinks Disponibles

#### 1. Console Sink
**Uso:** Desarrollo y debugging local

```json
{
  "Console": {
    "Enabled": true,
    "Format": "Json"  // "Text" o "Json"
  }
}
```

#### 2. Elasticsearch Sink
**Uso:** Producción, búsqueda y análisis de logs

```json
{
  "Elasticsearch": {
    "Enabled": true,
    "BaseUrl": "http://localhost:9200",
    "IndexName": "logs",
    "Authentication": {
      "Type": "Basic",
      "Username": "elastic",
      "Password": "changeme"
    },
    "BatchSize": 100,
    "TimeoutSeconds": 30
  }
}
```

#### 3. Kafka Sink
**Uso:** Arquitecturas de microservicios, procesamiento asíncrono

```json
{
  "Kafka": {
    "Enabled": true,
    "BootstrapServers": "localhost:9092",
    "Topic": "logs",
    "BatchSize": 100
  }
}
```

#### 4. HTTP Sink
**Uso:** Integración con sistemas personalizados

```json
{
  "Http": {
    "Enabled": true,
    "EndpointUrl": "https://api.example.com/logs",
    "BatchSize": 100,
    "TimeoutSeconds": 30
  }
}
```

#### 5. OpenTelemetry Sink
**Uso:** Ecosistema OpenTelemetry, integración con OTLP

```json
{
  "OpenTelemetry": {
    "Enabled": true,
    "Endpoint": "http://localhost:4318",
    "Protocol": "HttpJson",  // "HttpProtobuf", "HttpJson", "Grpc"
    "EnableCompression": true,
    "TimeoutSeconds": 30
  }
}
```

#### 6. Serilog Sink
**Uso:** Integración con ecosistema Serilog existente

```json
{
  "Serilog": {
    "Enabled": true
  }
}
```

### 🔍 Filtrado Avanzado

```json
{
  "Logging": {
    "Filters": {
      "MinLevel": "Warning",
      "MaxLevel": "Critical",
      "ExcludedCategories": ["Microsoft.AspNetCore"],
      "AllowedCategories": ["MyApp"],
      "ExcludedOperations": ["HealthCheck"],
      "ExcludedMessagePatterns": ["(?i)password", "(?i)secret"]
    }
  }
}
```

### 🎨 Enriquecimiento Automático

El logging se enriquece automáticamente con:
- ✅ **Ambiente** (Environment): Development, Staging, Production
- ✅ **Versión** (Version): Versión de la aplicación
- ✅ **Nombre del Servicio** (ServiceName): Nombre del microservicio
- ✅ **Nombre de la Máquina** (MachineName): Hostname
- ✅ **Información del Proceso** (ProcessId, ProcessName)
- ✅ **Información del Thread** (ThreadId, ThreadName)
- ✅ **Información del Usuario** (UserId, UserName)
- ✅ **CorrelationId** (automático desde contexto) 🆕 **v1.0.0**
- ✅ **TraceId y SpanId** (automático desde contexto) 🆕 **v1.0.0**

---

## Metrics

### 📚 Conceptos Básicos

**Metrics** son medidas numéricas que representan el estado y comportamiento de tu aplicación. JonjubNet.Observability soporta 4 tipos de métricas: Counters, Gauges, Histograms y Summaries.

### 🔧 Inyección de Dependencias

```csharp
public class OrderService
{
    private readonly IMetricsClient _metrics;

    public OrderService(IMetricsClient metrics)
    {
        _metrics = metrics;
    }
}
```

### 📊 Tipos de Métricas

#### 1. Counters (Contadores)
**Uso:** Contar eventos que solo aumentan

```csharp
// Incrementar contador
_metrics.Increment("orders_created_total", 1.0);

// Con tags
_metrics.Increment("orders_created_total", 1.0, new Dictionary<string, string>
{
    { "status", "success" },
    { "region", "us-east-1" }
});
```

#### 2. Gauges (Medidores)
**Uso:** Medir valores que pueden subir o bajar

```csharp
// Establecer gauge
_metrics.SetGauge("active_connections", 42.0);

// Con tags
_metrics.SetGauge("active_connections", 42.0, new Dictionary<string, string>
{
    { "server", "web-01" }
});
```

#### 3. Histograms (Histogramas)
**Uso:** Medir distribución de valores (latencias, tamaños, etc.)

```csharp
// Observar valor en histograma
_metrics.ObserveHistogram("request_duration_seconds", 0.234);

// Con tags
_metrics.ObserveHistogram("request_duration_seconds", 0.234, new Dictionary<string, string>
{
    { "method", "GET" },
    { "endpoint", "/api/orders" }
});
```

#### 4. Summaries (Resúmenes)
**Uso:** Calcular percentiles de valores observados

```csharp
// Observar valor en summary
_metrics.ObserveSummary("request_duration_seconds", 0.234);

// Con tags
_metrics.ObserveSummary("request_duration_seconds", 0.234, new Dictionary<string, string>
{
    { "method", "GET" },
    { "endpoint", "/api/orders" }
});
```

#### 5. Timers (Temporizadores)
**Uso:** Medir duración de operaciones automáticamente

```csharp
// Timer automático
using (_metrics.StartTimer("order_processing_duration_seconds", new Dictionary<string, string>
{
    { "order_type", "standard" }
}))
{
    // Tu lógica aquí
    // La duración se mide automáticamente
    await ProcessOrderAsync(order);
}
```

### 💡 Ejemplos Prácticos

#### Ejemplo 1: Métricas de Negocio
```csharp
public async Task<Order> CreateOrderAsync(OrderRequest request)
{
    // Contador de órdenes procesadas
    _metrics.Increment("orders_processed_total", 1.0, new Dictionary<string, string>
    {
        { "status", "processing" }
    });

    try
    {
        var order = await ProcessOrderAsync(request);
        
        // Contador de órdenes exitosas
        _metrics.Increment("orders_processed_total", 1.0, new Dictionary<string, string>
        {
            { "status", "success" }
        });
        
        return order;
    }
    catch (Exception ex)
    {
        // Contador de órdenes fallidas
        _metrics.Increment("orders_processed_total", 1.0, new Dictionary<string, string>
        {
            { "status", "error" }
        });
        throw;
    }
}
```

#### Ejemplo 2: Métricas de Performance
```csharp
public async Task<Order> GetOrderAsync(string orderId)
{
    using (_metrics.StartTimer("order_get_duration_seconds", new Dictionary<string, string>
    {
        { "operation", "get_order" }
    }))
    {
        var order = await _repository.GetByIdAsync(orderId);
        
        // Histograma de tamaño de respuesta
        _metrics.ObserveHistogram("order_response_size_bytes", 
            Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(order)));
        
        return order;
    }
}
```

### 🎯 Sinks Disponibles

#### 1. Prometheus Sink
**Uso:** Monitoreo estándar, integración con Grafana

```json
{
  "Prometheus": {
    "Enabled": true,
    "Endpoint": "/metrics"
  }
}
```

Accede a las métricas en: `http://localhost:5000/metrics`

#### 2. InfluxDB Sink
**Uso:** Series de tiempo, análisis temporal

```json
{
  "InfluxDB": {
    "Enabled": true,
    "Url": "http://localhost:8086",
    "Token": "your-token",
    "Bucket": "metrics",
    "Organization": "my-org"
  }
}
```

#### 3. StatsD Sink
**Uso:** Integración con Datadog, New Relic, etc.

```json
{
  "StatsD": {
    "Enabled": true,
    "Host": "localhost",
    "Port": 8125
  }
}
```

#### 4. Kafka Sink
**Uso:** Arquitecturas de microservicios, procesamiento asíncrono

```json
{
  "Kafka": {
    "Enabled": true,
    "BootstrapServers": "localhost:9092",
    "Topic": "metrics",
    "BatchSize": 100
  }
}
```

#### 5. OpenTelemetry Sink
**Uso:** Ecosistema OpenTelemetry, integración con OTLP

```json
{
  "OpenTelemetry": {
    "Enabled": true,
    "Endpoint": "http://localhost:4318",
    "Protocol": "HttpJson"
  }
}
```

#### 6. Elasticsearch Sink
**Uso:** Búsqueda y análisis de métricas

```json
{
  "Elasticsearch": {
    "Enabled": true,
    "BaseUrl": "http://localhost:9200",
    "IndexName": "metrics"
  }
}
```

#### 7. HTTP Sink
**Uso:** Integración con sistemas personalizados

```json
{
  "Http": {
    "Enabled": true,
    "EndpointUrl": "https://api.example.com/metrics",
    "BatchSize": 100
  }
}
```

### 🎨 Enriquecimiento Automático

Las métricas se enriquecen automáticamente con tags:
- ✅ `correlation.id` (automático desde contexto) 🆕 **v1.0.0**
- ✅ `trace.id` (automático desde contexto) 🆕 **v1.0.0**
- ✅ `span.id` (automático desde contexto) 🆕 **v1.0.0**
- ✅ `request.id` (automático desde contexto) 🆕 **v1.0.0**

---

## Tracing

### 📚 Conceptos Básicos

**Tracing** es el seguimiento de requests a través de múltiples servicios. JonjubNet.Observability proporciona tracing distribuido con soporte para spans, traces y correlación automática.

### 🔧 Inyección de Dependencias

```csharp
public class OrderService
{
    private readonly ITracingClient _tracing;

    public OrderService(ITracingClient tracing)
    {
        _tracing = tracing;
    }
}
```

### 📊 Crear Spans

#### Span Simple
```csharp
using var span = _tracing.StartSpan("process_order", SpanKind.Server);
```

#### Span con Tags
```csharp
using var span = _tracing.StartSpan("process_order", SpanKind.Server, 
    tags: new Dictionary<string, string>
    {
        { "order.id", orderId },
        { "customer.id", customerId }
    });
```

#### Span Hijo (Child Span)
```csharp
using var parentSpan = _tracing.StartSpan("process_order", SpanKind.Server);
using var childSpan = _tracing.StartChildSpan("validate_order", SpanKind.Internal);
```

### 📋 Tipos de Spans (SpanKind)

| Tipo | Uso | Ejemplo |
|------|-----|---------|
| **Server** | Request entrante (servidor) | `SpanKind.Server` |
| **Client** | Request saliente (cliente) | `SpanKind.Client` |
| **Internal** | Operación interna | `SpanKind.Internal` |
| **Producer** | Mensaje producido (Kafka, RabbitMQ) | `SpanKind.Producer` |
| **Consumer** | Mensaje consumido (Kafka, RabbitMQ) | `SpanKind.Consumer` |

### 📊 Estados de Spans (SpanStatus)

```csharp
span.Status = SpanStatus.Ok;        // Operación exitosa
span.Status = SpanStatus.Error;     // Operación fallida
span.Status = SpanStatus.Unset;     // Estado no establecido
```

### 🏷️ Agregar Tags y Events

```csharp
// Agregar tags
span.SetTag("order.id", orderId);
span.SetTag("customer.id", customerId);
span.SetTags(new Dictionary<string, string>
{
    { "order.amount", order.Amount.ToString() },
    { "order.currency", order.Currency }
});

// Agregar eventos
span.AddEvent("order.validated", new Dictionary<string, string>
{
    { "validation.time", DateTimeOffset.UtcNow.ToString() }
});

// Registrar excepción
span.RecordException(exception);
```

### 💡 Ejemplos Prácticos

#### Ejemplo 1: Tracing de Operación Completa
```csharp
public async Task<Order> CreateOrderAsync(OrderRequest request)
{
    using var span = _tracing.StartSpan("create_order", SpanKind.Server, 
        tags: new Dictionary<string, string>
        {
            { "customer.id", request.CustomerId },
            { "order.amount", request.Amount.ToString() }
        });
    
    try
    {
        span.AddEvent("order.validation.started");
        await ValidateOrderAsync(request);
        span.AddEvent("order.validation.completed");
        
        span.AddEvent("order.processing.started");
        var order = await ProcessOrderAsync(request);
        span.AddEvent("order.processing.completed");
        
        span.Status = SpanStatus.Ok;
        return order;
    }
    catch (Exception ex)
    {
        span.Status = SpanStatus.Error;
        span.RecordException(ex);
        throw;
    }
}
```

#### Ejemplo 2: Tracing con Spans Anidados
```csharp
public async Task<Order> ProcessOrderAsync(OrderRequest request)
{
    using var parentSpan = _tracing.StartSpan("process_order", SpanKind.Internal);
    
    using (var validateSpan = _tracing.StartChildSpan("validate_order", SpanKind.Internal))
    {
        await ValidateOrderAsync(request);
        validateSpan.Status = SpanStatus.Ok;
    }
    
    using (var paymentSpan = _tracing.StartChildSpan("process_payment", SpanKind.Internal))
    {
        await ProcessPaymentAsync(request);
        paymentSpan.Status = SpanStatus.Ok;
    }
    
    using (var fulfillSpan = _tracing.StartChildSpan("fulfill_order", SpanKind.Internal))
    {
        await FulfillOrderAsync(request);
        fulfillSpan.Status = SpanStatus.Ok;
    }
    
    parentSpan.Status = SpanStatus.Ok;
}
```

### 🎯 Exporters Disponibles

#### 1. OpenTelemetry Exporter
**Uso:** Ecosistema OpenTelemetry, integración con Jaeger, Zipkin, etc.

```json
{
  "OpenTelemetry": {
    "Enabled": true,
    "Endpoint": "http://localhost:4318",
    "Protocol": "HttpJson"
  }
}
```

#### 2. Kafka Exporter
**Uso:** Arquitecturas de microservicios, procesamiento asíncrono

```json
{
  "Kafka": {
    "Enabled": true,
    "BootstrapServers": "localhost:9092",
    "Topic": "traces",
    "BatchSize": 100
  }
}
```

#### 3. Elasticsearch Exporter
**Uso:** Búsqueda y análisis de traces

```json
{
  "Elasticsearch": {
    "Enabled": true,
    "BaseUrl": "http://localhost:9200",
    "IndexName": "traces"
  }
}
```

#### 4. HTTP Exporter
**Uso:** Integración con sistemas personalizados

```json
{
  "Http": {
    "Enabled": true,
    "EndpointUrl": "https://api.example.com/traces",
    "BatchSize": 100
  }
}
```

### 🎨 Enriquecimiento Automático

Los spans se enriquecen automáticamente con:
- ✅ `CorrelationId` (automático desde contexto) 🆕 **v1.0.0**
- ✅ `TraceId` (automático desde contexto) 🆕 **v1.0.0**
- ✅ `SpanId` (automático desde contexto) 🆕 **v1.0.0**
- ✅ `RequestId` (automático desde contexto) 🆕 **v1.0.0**
- ✅ `ServiceName` (desde configuración)
- ✅ `ResourceName` (desde configuración)

---

## Correlación y Trazabilidad

### 📚 Conceptos Básicos

La **correlación** permite rastrear una transacción a través de múltiples servicios y componentes. JonjubNet.Observability proporciona correlación automática en todos los protocolos.

### 🔑 CorrelationId

El `CorrelationId` es el identificador único de una transacción que se propaga automáticamente entre servicios.

#### Propagación Automática en HTTP

El middleware HTTP automáticamente:
1. ✅ Lee `X-Correlation-Id` del header entrante (si existe)
2. ✅ Genera uno nuevo si no existe
3. ✅ Establece en `ObservabilityContext`
4. ✅ Propaga en headers de respuesta
5. ✅ Propaga en requests salientes (via `CorrelationDelegatingHandler`)

#### Propagación Automática en Kafka

`KafkaNativeProducer` automáticamente:
1. ✅ Lee `CorrelationId` del `ObservabilityContext`
2. ✅ Agrega `X-Correlation-Id` en headers de Kafka
3. ✅ Los consumidores pueden leer `CorrelationId` de headers

#### Propagación Manual

```csharp
// Establecer CorrelationId manualmente
ObservabilityContext.SetCorrelationId("my-correlation-id");

// Obtener CorrelationId actual
var correlationId = ObservabilityContext.Current?.CorrelationId;
```

### 🌐 Protocolos Soportados

#### HTTP/REST

```csharp
// Automático via CorrelationDelegatingHandler
// Configurar HttpClient para usar el handler
services.AddHttpClient<MyService>()
    .AddHttpMessageHandler<CorrelationDelegatingHandler>();
```

#### Kafka

```csharp
// Automático en KafkaNativeProducer
// CorrelationId se agrega en headers de Kafka automáticamente
var producer = new KafkaNativeProducer(options);
await producer.SendAsync(topic, message); // CorrelationId agregado automáticamente
```

#### gRPC

```csharp
// Usar GrpcClientCorrelationInterceptor
var channel = GrpcChannel.ForAddress("https://localhost:5001");
var client = new MyServiceClient(channel.Intercept(new GrpcClientCorrelationInterceptor()));

// En el servidor, usar GrpcServerCorrelationInterceptor
services.AddGrpc(options =>
{
    options.Interceptors.Add<GrpcServerCorrelationInterceptor>();
});
```

#### RabbitMQ

```csharp
// Usar RabbitMqCorrelationHelper
var properties = channel.CreateBasicProperties();
RabbitMqCorrelationHelper.AddCorrelationIdToProperties(properties.Headers);
```

#### Azure Service Bus

```csharp
// Usar AzureServiceBusCorrelationHelper
var message = new Message(body);
AzureServiceBusCorrelationHelper.AddCorrelationIdToApplicationProperties(message.ApplicationProperties);
```

#### SignalR

```csharp
// Usar SignalRCorrelationMiddleware
app.UseMiddleware<SignalRCorrelationMiddleware>();
```

---

## Configuración Avanzada

### 🔄 Configuración Dinámica (Hot-Reload)

Los cambios en `appsettings.json` se aplican automáticamente sin reiniciar la aplicación:

```json
{
  "Logging": {
    "Sinks": {
      "Console": {
        "Enabled": true  // Cambiar a false y se aplica automáticamente
      }
    }
  }
}
```

### 🔀 Múltiples Sinks Simultáneos

Puedes habilitar múltiples sinks simultáneamente:

```json
{
  "Logging": {
    "Sinks": {
      "Console": { "Enabled": true },
      "Elasticsearch": { "Enabled": true },
      "Kafka": { "Enabled": true },
      "OpenTelemetry": { "Enabled": true }
    }
  }
}
```

### 💾 Dead Letter Queue

```json
{
  "Logging": {
    "DeadLetterQueue": {
      "Enabled": true,
      "MaxSize": 10000,
      "Encryption": {
        "EnableAtRest": true,
        "Key": "your-encryption-key"
      }
    }
  }
}
```

### 🔁 Retry Policy

```json
{
  "Logging": {
    "RetryPolicy": {
      "MaxAttempts": 3,
      "InitialDelaySeconds": 1,
      "MaxDelaySeconds": 30,
      "BackoffMultiplier": 2.0,
      "JitterEnabled": true
    }
  }
}
```

### ⚡ Circuit Breaker

```json
{
  "Logging": {
    "CircuitBreaker": {
      "FailureThreshold": 5,
      "SuccessThreshold": 2,
      "TimeoutSeconds": 60
    }
  }
}
```

---

## Mejores Prácticas

### 1. ✅ Usar CorrelationId Consistentemente

```csharp
// ✅ CORRECTO: CorrelationId se propaga automáticamente
// No necesitas hacer nada, el middleware lo maneja

// ❌ INCORRECTO: No establecer CorrelationId manualmente a menos que sea necesario
ObservabilityContext.SetCorrelationId("manual-id"); // Solo si es absolutamente necesario
```

### 2. ✅ Usar Tags Apropiados

```csharp
// ✅ CORRECTO: Tags descriptivos y consistentes
_metrics.Increment("orders_created_total", 1.0, new Dictionary<string, string>
{
    { "status", "success" },
    { "region", "us-east-1" },
    { "service", "order-service" }
});

// ❌ INCORRECTO: Tags inconsistentes o muy específicos
_metrics.Increment("orders_created_total", 1.0, new Dictionary<string, string>
{
    { "order_id", orderId },  // Muy específico, no útil para agregación
    { "timestamp", DateTimeOffset.UtcNow.ToString() }  // No es un tag útil
});
```

### 3. ✅ Usar Niveles de Log Apropiados

```csharp
// ✅ CORRECTO
_logging.LogTrace("Very detailed debug info");  // Solo para desarrollo
_logging.LogDebug("Debug information");          // Desarrollo y troubleshooting
_logging.LogInformation("Business event");       // Eventos de negocio importantes
_logging.LogWarning("Potential issue");          // Advertencias
_logging.LogError(exception, "Error occurred"); // Errores
_logging.LogCritical(exception, "System failure"); // Fallos críticos

// ❌ INCORRECTO
_logging.LogError("This is just info");  // No es un error
_logging.LogInformation(exception, "Error");  // Debe ser LogError
```

### 4. ✅ Usar Spans para Operaciones Importantes

```csharp
// ✅ CORRECTO: Spans para operaciones importantes
using var span = _tracing.StartSpan("process_order", SpanKind.Server);
// ... lógica de negocio ...
span.Status = SpanStatus.Ok;

// ❌ INCORRECTO: Spans para operaciones triviales
using var span = _tracing.StartSpan("get_config_value");  // Demasiado granular
```

### 5. ✅ Configurar Límites Apropiados

```json
{
  "Logging": {
    "Registry": {
      "MaxSize": 10000  // Ajustar según necesidades
    }
  },
  "Metrics": {
    "Registry": {
      "MaxSize": 50000  // Métricas pueden ser más numerosas
    }
  },
  "Tracing": {
    "Registry": {
      "MaxSize": 5000  // Traces son más grandes
    }
  }
}
```

---

## Troubleshooting

### ❌ Problema: Los logs no aparecen

**Solución:**
1. ✅ Verificar que el sink esté habilitado en configuración
2. ✅ Verificar que el nivel de log sea apropiado
3. ✅ Verificar conectividad con el destino (Elasticsearch, Kafka, etc.)
4. ✅ Revisar logs de error en la aplicación
5. ✅ Verificar Dead Letter Queue para logs fallidos

### ❌ Problema: Las métricas no se exportan

**Solución:**
1. ✅ Verificar que el sink esté habilitado
2. ✅ Verificar que el endpoint sea accesible (Prometheus, InfluxDB, etc.)
3. ✅ Verificar configuración de autenticación si es necesaria
4. ✅ Revisar Dead Letter Queue para métricas fallidas
5. ✅ Verificar que el endpoint de métricas sea accesible: `http://localhost:5000/metrics`

### ❌ Problema: Los traces no se correlacionan

**Solución:**
1. ✅ Verificar que `UseJonjubNetObservability()` esté configurado
2. ✅ Verificar que `CorrelationId` se propague en headers HTTP
3. ✅ Verificar que los servicios downstream lean el `CorrelationId`
4. ✅ Revisar logs para ver si `CorrelationId` se está generando
5. ✅ Verificar que `ObservabilityContext` esté configurado correctamente

### ❌ Problema: Alto uso de memoria

**Solución:**
1. ✅ Reducir `MaxSize` en registries
2. ✅ Aumentar frecuencia de exportación
3. ✅ Verificar que los sinks estén funcionando (revisar DLQ)
4. ✅ Revisar si hay memory leaks en código personalizado
5. ✅ Verificar que los límites de tamaño estén configurados apropiadamente

### ❌ Problema: Performance degradado

**Solución:**
1. ✅ Verificar que no haya demasiados sinks habilitados simultáneamente
2. ✅ Verificar configuración de batching
3. ✅ Verificar que los destinos (Elasticsearch, Kafka, etc.) estén respondiendo rápidamente
4. ✅ Revisar logs de error para identificar cuellos de botella
5. ✅ Considerar reducir el `SampleRate` en el middleware HTTP

---

## Recursos Adicionales

### 📚 Documentación

- **[README.md](../README.md)**: Visión general completa del componente, análisis técnico profundo, comparación con otras soluciones y métricas de rendimiento
- **[Guía de Implementación Detallada](IMPLEMENTATION_GUIDE.md)**: Documentación técnica detallada con ejemplos avanzados y configuración por niveles
- **[Ejemplos de Configuración](../Presentation/JonjubNet.Observability/appsettings.example.json)**: Configuraciones de ejemplo para todos los sinks y exporters

### 🔧 Infraestructura

- **Infraestructura Necesaria**: Consulta la sección "Infraestructura Necesaria por Sink" en [IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md)
- **Configuración de Sinks**: Ejemplos detallados de configuración para cada sink en [IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md)

### 💡 Mejores Prácticas

- **Mejores Prácticas**: Revisa la sección "Mejores Prácticas" en esta guía
- **Troubleshooting**: Consulta la sección "Troubleshooting" en esta guía

### 🆕 Novedades v1.0.0

- **Correlación Automática**: Implementación completa de correlación automática en todos los protocolos
- **ObservabilityContext**: Contexto compartido usando AsyncLocal para propagación thread-safe
- **Middleware HTTP**: Correlación automática en requests entrantes y salientes

---

## Conclusión

Esta guía cubre la implementación completa de Logging, Metrics y Tracing con JonjubNet.Observability. 

### Próximos Pasos

1. **Revisar [README.md](../README.md)**: Para una visión general completa del componente, comparación con otras soluciones y métricas de rendimiento
2. **Consultar [IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md)**: Para documentación técnica detallada y ejemplos avanzados
3. **Explorar [appsettings.example.json](../Presentation/JonjubNet.Observability/appsettings.example.json)**: Para ejemplos de configuración de todos los sinks

**¡Feliz Observabilidad!** 🚀

---

**Versión:** 1.0.0 | **Última actualización:** Diciembre 2024
