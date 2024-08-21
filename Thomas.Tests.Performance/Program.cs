using BenchmarkDotNet.Running;
using Thomas.Tests.Performance.Benchmark;

namespace Thomas.Tests.Performance
{
    class Program
    {
        public const string BenchmarkResultsPath = "BenchmarkResults";
        static void Main(string[] args)
        {
            new BenchmarkSwitcher(typeof(BenckmarkBase).Assembly).Run(args, new BenchmarkConfig());
        }
    }
}
