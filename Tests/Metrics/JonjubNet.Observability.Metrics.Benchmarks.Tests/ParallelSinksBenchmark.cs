#pragma warning disable CS8602 // Desreferencia de una referencia posiblemente NULL
#pragma warning disable CS8603 // Posible tipo de valor devuelto de referencia nulo
#pragma warning disable CS8619 // La nulabilidad de los tipos de referencia en el valor no coincide con el tipo de destino
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace JonjubNet.Observability.Metrics.Benchmarks.Tests
{
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    public class ParallelSinksBenchmark
    {
        private List<IMetricsSink> _sinks = null!;
        private MetricRegistry _registry = null!;

        [GlobalSetup]
        public void Setup()
        {
            _registry = new MetricRegistry();
            _sinks = new List<IMetricsSink>();
            for (int i = 0; i < 5; i++)
            {
                var mockSink = new Mock<IMetricsSink>();
                mockSink.Setup(s => s.Name).Returns($"Sink{i}");
                mockSink.Setup(s => s.IsEnabled).Returns(true);
                mockSink.Setup(s => s.ExportFromRegistryAsync(It.IsAny<MetricRegistry>(), It.IsAny<CancellationToken>()))
                    .Returns(ValueTask.CompletedTask);
                var sink = mockSink.Object;
                if (sink != null)
                {
                    _sinks.Add(sink);
                }
            }

            // Crear métricas en el registry
            for (int i = 0; i < 100; i++)
            {
                var counter = _registry!.GetOrCreateCounter($"metric_{i}", "");
                counter!.Inc(value: 1.0);
            }
        }

        [Benchmark(Baseline = true)]
        public async Task FlushSinks_Sequential()
        {
            foreach (var sink in _sinks!)
            {
                await sink.ExportFromRegistryAsync(_registry!, CancellationToken.None);
            }
        }

        [Benchmark]
        public async Task FlushSinks_Parallel()
        {
            var tasks = _sinks!.Select(sink => sink.ExportFromRegistryAsync(_registry!, CancellationToken.None).AsTask());
            await Task.WhenAll(tasks);
        }
    }
}

