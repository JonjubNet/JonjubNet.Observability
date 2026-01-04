#pragma warning disable CS8602 // Desreferencia de una referencia posiblemente NULL
#pragma warning disable CS8603 // Posible tipo de valor devuelto de referencia nulo
#pragma warning disable CS8619 // La nulabilidad de los tipos de referencia en el valor no coincide con el tipo de destino
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.Core.MetricTypes;
using Xunit;

namespace JonjubNet.Observability.Metrics.Core.Tests.Performance
{
    /// <summary>
    /// Tests de performance para componentes de métricas
    /// Ejecutar con: dotnet run --project Tests/Metrics/JonjubNet.Observability.Metrics.Core.Tests -c Release
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net80)]
    public class MetricsPerformanceTests
    {
        private MetricRegistry _registry = null!;
        private MetricsClient _client = null!;
        private Counter _counter = null!;
        private Gauge _gauge = null!;
        private Histogram _histogram = null!;

        [GlobalSetup]
        private void Setup()
        {
            _registry = new MetricRegistry();
            _client = new MetricsClient(_registry);
            _counter = _registry.GetOrCreateCounter("test_counter", "Test counter");
            _gauge = _registry.GetOrCreateGauge("test_gauge", "Test gauge");
            _histogram = _registry.GetOrCreateHistogram("test_histogram", "Test histogram");
            
            // Asegurar que los campos no sean null después de la inicialización
            if (_counter == null || _gauge == null || _histogram == null || _client == null || _registry == null)
            {
                throw new InvalidOperationException("Failed to initialize benchmark fields");
            }
        }

        [Benchmark]
        private void Counter_Increment()
        {
            _counter!.Inc(value: 1.0);
        }

        [Benchmark]
        private void Counter_Increment_WithTags()
        {
            var tags = new Dictionary<string, string> { ["env"] = "prod", ["service"] = "api" };
            _counter!.Inc(tags, 1.0);
        }

        [Benchmark]
        private void Gauge_Set()
        {
            _gauge!.Set(value: 42.5);
        }

        [Benchmark]
        private void Histogram_Observe()
        {
            _histogram!.Observe(value: 10.5);
        }

        [Benchmark]
        private void MetricsClient_Increment()
        {
            _client!.Increment("benchmark_counter", 1.0);
        }

        [Benchmark]
        private void MetricRegistry_GetOrCreateCounter()
        {
            _registry!.GetOrCreateCounter("new_counter", "Description");
        }

        [Fact]
        public void RunBenchmarks()
        {
            // Este test puede ejecutarse manualmente para ver los resultados
            // BenchmarkRunner.Run<MetricsPerformanceTests>();
        }
    }
}

