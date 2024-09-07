#if NETCOREAPP
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using System.ComponentModel;
using System.Linq;
using Thomas.Tests.Performance.Entities;
using Microsoft.EntityFrameworkCore;

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

        [Benchmark(Description = "ToList<T>")]
        public void QueryListUnbuffered()
        {
            new PersonContext(StringConnection).People.ToList().Consume(consumer);
        }

        [Benchmark(Description = "SqlQueryRaw<T>")]
        public void SqlQueryRaw()
        {
            new PersonContext(StringConnection).Database.SqlQueryRaw<Person>($"SELECT * FROM {TableName}").Consume(consumer);
        }
    }
}
#endif