using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using System.ComponentModel;
using System.Linq;

namespace Thomas.Tests.Performance.Benchmark.Others
{
    [Description("EntityFramework")]
    [ThreadingDiagnoser]
    [MemoryDiagnoser]
    public class EntityFrameworkBenckmark : BenckmarkBase
    {
        private readonly Consumer consumer = new Consumer();

        [GlobalSetup]
        public void Setup()
        {
            Start();
        }

        [Benchmark(Description = "ToList (unbuffered)")]
        public void QueryListUnbuffered()
        {
            using var context = new PersonContext(StringConnection);
            context.People.Where(x => x.Id > 0).ToList().Consume(consumer);
        }
    }
}
