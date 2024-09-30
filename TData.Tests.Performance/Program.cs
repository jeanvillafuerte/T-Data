using BenchmarkDotNet.Running;
using TData.Tests.Performance.Benchmark;

namespace TData.Tests.Performance
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
