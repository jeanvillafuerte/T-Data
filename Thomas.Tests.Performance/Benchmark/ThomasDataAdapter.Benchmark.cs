using BenchmarkDotNet.Attributes;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Benchmark
{
    [Description("ThomasDataAdapter")]
    [ThreadingDiagnoser]
    [MemoryDiagnoser]
    public class ThomasDataAdapterBenckmark : Setup
    {
        [GlobalSetup]
        public void Setup()
        {
            Start();
        }

        [Benchmark(Description = "ExecuteOp Resilient error")]
        public void ToListError()
        {
            service.ExecuteOp($"UPDATE {TableName} SET UserName2 = 'sample 2' WHERE Id = 1;", false);
        }

        [Benchmark(Description = "ToSingleOp<> Resilient error")]
        public void ToSingleOpError()
        {
            service.ToSingleOp<Person>($"SELECT UserName2 FROM {TableName} WHERE Id = 1;", false);
        }

        [Benchmark(Description = "ToListOp<> Resilient error")]
        public void ToListOpError()
        {
            service.ToListOp<Person>($"SELECT UserName2 FROM {TableName} WHERE Id = 1;", false);
        }

        [Benchmark(Description = "ToList<>")]
        public void ToList()
        {
            service.ToList<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        }

        [Benchmark(Description = "Single<>")]
        public void Single()
        {
            service.ToSingle<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        }

        [Benchmark(Description = "ToListAsync<>")]
        public async Task ToListAsync()
        {
            await service.ToListAsync<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false, CancellationToken.None);
        }

        [Benchmark(Description = "SingleAsync<>")]
        public async Task SingleAsync()
        {
           await service.ToSingleAsync<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false, CancellationToken.None);
        }

        //[Benchmark(Description = "ToTuple<>")]
        //public void ToTuple()
        //{
        //    service.ToTuple<Person, Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;" + $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        //}

        [Benchmark(Description = "ToList<> T with nullables")]
        public void ToList2()
        {
            service.ToList<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        }

        [Benchmark(Description = "Single<> T with nullables")]
        public void Single2()
        {
            service.ToSingle<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        }

        [Benchmark(Description = "ToListAsync<> T with nullables")]
        public async Task ToListAsync2()
        {
            await service.ToListAsync<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false, CancellationToken.None);
        }

        [Benchmark(Description = "SingleAsync<> T with nullables")]
        public async Task SingleAsync2()
        {
            await service.ToSingleAsync<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false, CancellationToken.None);
        }

        //[Benchmark(Description = "ToTuple<> T with nullables")]
        //public void ToTuple2()
        //{
        //    service.ToTuple<PersonWithNullables, PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;" + $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        //}

        [Benchmark(Description = "ToListOp<>")]
        public void ToListOp()
        {
            service.ToListOp<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        }

        [Benchmark(Description = "SingleOp<>")]
        public void SingleOp()
        {
            service.ToSingleOp<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        }

        [Benchmark(Description = "ToListOp<>")]
        public async Task ToListOpAsync()
        {
            await service.ToListOpAsync<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false, CancellationToken.None);
        }

        [Benchmark(Description = "SingleOp<>")]
        public async Task SingleOpAsync()
        {
            await service.ToSingleOpAsync<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false, CancellationToken.None);
        }

        //[Benchmark(Description = "ToTupleOp<>")]
        //public void ToTupleOp()
        //{
        //    service.ToTuple<Person, Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;" + $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        //}

        [Benchmark(Description = "ToListOp<> T with nullables")]
        public void ToList2op()
        {
            service.ToListOp<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        }

        [Benchmark(Description = "ToListOp<> T from store procedure")]
        public void ToList3op()
        {
            service.ToListOp<Person>(new { age = 5 }, "get_persons");
        }

        [Benchmark(Description = "ToListOp<> T with nullables from store procedure")]
        public void ToList4op()
        {
            service.ToListOp<PersonWithNullables>(new { age = 5 }, "get_persons");
        }

        [Benchmark(Description = "SingleOp<> T with nullables")]
        public void Single2Op()
        {
            service.ToSingleOp<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        }

        //[Benchmark(Description = "ToTupleOp<> T with nullables")]
        //public void ToTuple2Op()
        //{
        //    service.ToTuple<PersonWithNullables, PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;" + $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        //}

        [Benchmark(Description = "Execute with search term")]
        public void Execute()
        {
            var searchTerm = new SearchTerm(id: 1);
            service.Execute(searchTerm, "get_byId");
        }

        [Benchmark(Description = "ExecuteOp with search term")]
        public void ExecuteOp()
        {
            var searchTerm = new SearchTerm(id: 1);
            service.ExecuteOp(searchTerm, "get_byId");
        }

        [Benchmark(Description = "Execute with search term")]
        public async Task ExecuteAsync()
        {
            var searchTerm = new SearchTerm(id: 1);
            await service.ExecuteAsync(searchTerm, "get_byId", CancellationToken.None);
        }

        [Benchmark(Description = "ExecuteOp with search term")]
        public async Task ExecuteOpAsync()
        {
            var searchTerm = new SearchTerm(id: 1);
            await service.ExecuteOpAsync(searchTerm, "get_byId", CancellationToken.None);
        }

        [GlobalCleanup]
        public void CleanTempData()
        {
            Clean();
        }
    }
}
