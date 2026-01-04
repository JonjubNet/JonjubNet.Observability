using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JonjubNet.Observability.Shared.Utils;
using System.Text;

namespace JonjubNet.Observability.Metrics.Benchmarks.Tests
{
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    public class CompressionBenchmark
    {
        private string _largeJson = null!;
        private byte[] _largeJsonBytes = null!;

        [GlobalSetup]
        public void Setup()
        {
            // Simular un payload grande de métricas
            var sb = new StringBuilder();
            sb.Append("{");
            for (int i = 0; i < 1000; i++)
            {
                sb.Append($"\"metric_{i}\": {{ \"value\": {i}, \"tags\": {{\"env\":\"prod\",\"service\":\"api\"}} }},");
            }
            sb.Append("}");
            _largeJson = sb.ToString();
            _largeJsonBytes = Encoding.UTF8.GetBytes(_largeJson);
        }

        [Benchmark(Baseline = true)]
        public byte[] NoCompression()
        {
            return _largeJsonBytes!;
        }

        [Benchmark]
        public byte[] CompressGZip()
        {
            return CompressionHelper.CompressGZip(_largeJsonBytes!);
        }

        [Benchmark]
        public byte[] CompressBrotli()
        {
            return CompressionHelper.CompressBrotli(_largeJsonBytes!);
        }

        [Benchmark]
        public byte[] CompressGZip_String()
        {
            return CompressionHelper.CompressGZipString(_largeJson!);
        }
    }
}

