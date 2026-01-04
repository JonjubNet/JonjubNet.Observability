using BenchmarkDotNet.Running;
using JonjubNet.Observability.Metrics.Benchmarks.Tests;

namespace JonjubNet.Observability.Metrics.Benchmarks.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Running JonjubNet.Observability.Metrics Benchmarks...");
            Console.WriteLine();

            var summary = BenchmarkRunner.Run(new[]
            {
                typeof(CounterBenchmark),
                typeof(MetricsClientBenchmark),
                typeof(PerformanceOptimizationsBenchmark)
            });

            Console.WriteLine();
            Console.WriteLine("Benchmarks completed!");
        }
    }
}
