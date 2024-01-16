using BenchmarkDotNet.Running;
using Thomas.Tests.Performance.Benchmark;

namespace Thomas.Tests.Performance
{
    class Program
    {
        static void Main(string[] args)
        {
            new BenchmarkSwitcher(typeof(BenckmarkBase).Assembly).Run(args, new BenchmarkConfig());
        }
    }
}
