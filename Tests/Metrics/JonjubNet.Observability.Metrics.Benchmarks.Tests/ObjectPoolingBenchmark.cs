using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.Core.Utils;

namespace JonjubNet.Observability.Metrics.Benchmarks.Tests
{
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    public class ObjectPoolingBenchmark
    {
        [Benchmark(Baseline = true)]
        public void CreateDictionary_WithoutPool()
        {
            for (int i = 0; i < 1000; i++)
            {
                var dict = new Dictionary<string, string>();
                dict["key"] = "value";
                // Simular uso
                _ = dict.Count;
            }
        }

        [Benchmark]
        public void CreateDictionary_WithPool()
        {
            for (int i = 0; i < 1000; i++)
            {
                var dict = CollectionPool.RentDictionary();
                try
                {
                    dict["key"] = "value";
                    // Simular uso
                    _ = dict.Count;
                }
                finally
                {
                    CollectionPool.ReturnDictionary(dict);
                }
            }
        }
    }
}

