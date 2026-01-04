using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JonjubNet.Observability.Metrics.Core.MetricTypes;

namespace JonjubNet.Observability.Metrics.Benchmarks.Tests
{
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    public class CounterBenchmark
    {
        private Counter _counter = null!;
        private Dictionary<string, string> _tags = null!;

        [GlobalSetup]
        public void Setup()
        {
            _counter = new Counter("test_counter", "Test counter");
            _tags = new Dictionary<string, string> { ["env"] = "prod", ["service"] = "api" };
        }

        [Benchmark]
        public void Increment_NoTags()
        {
            _counter!.Inc();
        }

        [Benchmark]
        public void Increment_WithTags()
        {
            _counter!.Inc(_tags!);
        }

        [Benchmark]
        public long GetValue_NoTags()
        {
            return _counter!.GetValue();
        }

        [Benchmark]
        public long GetValue_WithTags()
        {
            return _counter!.GetValue(_tags!);
        }
    }
}
