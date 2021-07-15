using BenchmarkDotNet.Attributes;
using System.ComponentModel;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Benchmark
{
    [Description("ThomasDataAdapter")]
    public class ThomasDataAdapterBenckmark : Setup
    {
        [GlobalSetup]
        public void Setup()
        {
            Start();
        }


        [Benchmark(Description = "ToList<>")]
        public void ToList()
        {
            service.ToList<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1", false);
        }

        [Benchmark(Description = "Single<>")]
        public void Single()
        {
            service.ToSingle<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1", false);
        }

        [Benchmark(Description = "ToTuple<>")]
        public void ToTuple()
        {
            service.ToTuple<Person, Person>($@" SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;
                                                SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        }

        [Benchmark(Description = "ToList<> T with nullables")]
        public void ToList2()
        {
            service.ToList<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1", false);
        }

        [Benchmark(Description = "Single<> T with nullables")]
        public void Single2()
        {
            service.ToSingle<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1", false);
        }

        [Benchmark(Description = "ToTuple<> T with nullables")]
        public void ToTuple2()
        {
            service.ToTuple<PersonWithNullables, PersonWithNullables>($@" SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;
                                                SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        }

        [GlobalCleanup]
        public void CleanTempData()
        {
            Clean();
        }
    }
}
