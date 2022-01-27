using BenchmarkDotNet.Attributes;
using System.ComponentModel;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Benchmark
{
    [Description("ThomasDataAdapter")]
    public class ThomasDataAdapterBenckmark : Setup
    {
        string Query
        {
            get
            {
                return $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;";
            }
        }

        [GlobalSetup]
        public void Setup()
        {
            Start();
        }


        [Benchmark(Description = "ToList<>")]
        public void ToList()
        {
            service.ToList<Person>(Query, false);
        }

        [Benchmark(Description = "Single<>")]
        public void Single()
        {
            service.ToSingle<Person>(Query, false);
        }

        [Benchmark(Description = "ToTuple<>")]
        public void ToTuple()
        {
            service.ToTuple<Person, Person>(Query + Query, false);
        }

        [Benchmark(Description = "ToList<> T with nullables")]
        public void ToList2()
        {
            service.ToList<PersonWithNullables>(Query, false);
        }

        [Benchmark(Description = "Single<> T with nullables")]
        public void Single2()
        {
            service.ToSingle<PersonWithNullables>(Query, false);
        }

        [Benchmark(Description = "ToTuple<> T with nullables")]
        public void ToTuple2()
        {
            service.ToTuple<PersonWithNullables, PersonWithNullables>(Query + Query, false);
        }

        [Benchmark(Description = "ToListOp<>")]
        public void ToListOp()
        {
            service.ToListOp<Person>(Query, false);
        }

        [Benchmark(Description = "SingleOp<>")]
        public void SingleOp()
        {
            service.ToSingle<Person>(Query, false);
        }

        [Benchmark(Description = "ToTupleOp<>")]
        public void ToTupleOp()
        {
            service.ToTuple<Person, Person>(Query + Query, false);
        }

        [Benchmark(Description = "ToListOp<> T with nullables")]
        public void ToList2op()
        {
            service.ToListOp<PersonWithNullables>(Query, false);
        }

        [Benchmark(Description = "SingleOp<> T with nullables")]
        public void Single2Op()
        {
            service.ToSingleOp<PersonWithNullables>(Query, false);
        }

        [Benchmark(Description = "ToTupleOp<> T with nullables")]
        public void ToTuple2Op()
        {
            service.ToTuple<PersonWithNullables, PersonWithNullables>(Query + Query, false);
        }

        [GlobalCleanup]
        public void CleanTempData()
        {
            Clean();
        }
    }
}
