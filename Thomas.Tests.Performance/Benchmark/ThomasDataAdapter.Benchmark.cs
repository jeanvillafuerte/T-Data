using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Thomas.Database;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Benchmark
{
    [Description("ThomasDataAdapter")]
    [ThreadingDiagnoser]
    [MemoryDiagnoser]
    public class ThomasDataAdapterBenckmark : Setup
    {
        private readonly Consumer consumer = new Consumer();

        [GlobalSetup]
        public void Setup()
        {
            Start();
        }

        [Benchmark(Description = "ToList<>")]
        public void ToList()
        {
            var list = service.ToList<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
            list.Consume(consumer);
        }

        [Benchmark(Description = "Single<>")]
        public Person Single()
        {
            return service.ToSingle<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        }

        [Benchmark(Description = "ToListAsync<>")]
        public async Task ToListAsync()
        {
            var result = await service.ToListAsync<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false, CancellationToken.None);
            result.Consume(consumer);
        }

        [Benchmark(Description = "SingleAsync<>")]
        public async Task<Person> SingleAsync()
        {
            return await service.ToSingleAsync<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false, CancellationToken.None);
        }

        [Benchmark(Description = "ToTuple<>")]
        public Tuple<IEnumerable<Person>, IEnumerable<Person>> ToTuple()
        {
            return service.ToTuple<Person, Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;" + $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        }

        [Benchmark(Description = "ToList<> T with nullables")]
        public void ToList2()
        {
            var result = service.ToList<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
            result.Consume(consumer);
        }

        [Benchmark(Description = "Single<> T with nullables")]
        public PersonWithNullables Single2()
        {
            return service.ToSingle<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        }

        [Benchmark(Description = "ToListAsync<> T with nullables")]
        public async Task ToListAsync2()
        {
            var result = await service.ToListAsync<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false, CancellationToken.None);
            result.Consume(consumer);
        }

        [Benchmark(Description = "SingleAsync<> T with nullables")]
        public async Task<PersonWithNullables> SingleAsync2()
        {
            return await service.ToSingleAsync<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false, CancellationToken.None);
        }

        [Benchmark(Description = "ToTuple<> T with nullables")]
        public Tuple<IEnumerable<PersonWithNullables>, IEnumerable<PersonWithNullables>> ToTuple2()
        {
            return service.ToTuple<PersonWithNullables, PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;" + $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        }

        [Benchmark(Description = "ToListOp<>")]
        public DbOpResult<IEnumerable<Person>> ToListOp()
        {
            return service.ToListOp<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        }

        [Benchmark(Description = "SingleOp<>")]
        public DbOpResult<Person> SingleOp()
        {
            return service.ToSingleOp<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        }

        [Benchmark(Description = "ToListOp<>")]
        public async Task<DbOpAsyncResult<IEnumerable<Person>>> ToListOpAsync()
        {
            return await service.ToListOpAsync<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false, CancellationToken.None);
        }

        [Benchmark(Description = "SingleOp<>")]
        public async Task<DbOpAsyncResult<Person>> SingleOpAsync()
        {
            return await service.ToSingleOpAsync<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false, CancellationToken.None);
        }

        [Benchmark(Description = "ToTupleOp<>")]
        public DbOpResult<Tuple<IEnumerable<Person>, IEnumerable<Person>>> ToTupleOp()
        {
            return service.ToTupleOp<Person, Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;" + $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        }

        [Benchmark(Description = "ToListOp<> T with nullables")]
        public DbOpResult<IEnumerable<PersonWithNullables>> ToList2op()
        {
            return service.ToListOp<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        }

        [Benchmark(Description = "ToListOp<> T from store procedure")]
        public DbOpResult<IEnumerable<Person>> ToList3op()
        {
            return service.ToListOp<Person>(new { age = 5 }, "get_persons");
        }

        [Benchmark(Description = "ToListOp<> T with nullables from store procedure")]
        public DbOpResult<IEnumerable<PersonWithNullables>> ToList4op()
        {
            return service.ToListOp<PersonWithNullables>(new { age = 5 }, "get_persons");
        }

        [Benchmark(Description = "SingleOp<> T with nullables")]
        public DbOpResult<PersonWithNullables> Single2Op()
        {
            return service.ToSingleOp<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        }

        [Benchmark(Description = "ToTupleOp<> T with nullables")]
        public DbOpResult<Tuple<IEnumerable<PersonWithNullables>, IEnumerable<PersonWithNullables>>> ToTuple2Op()
        {
            return service.ToTupleOp<PersonWithNullables, PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;" + $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", false);
        }

        [Benchmark(Description = "Execute with search term")]
        public int Execute()
        {
            var searchTerm = new SearchTerm(id: 1);
            return service.Execute(searchTerm, "get_byId");
        }

        [Benchmark(Description = "ExecuteOp with search term")]
        public DbOpResult ExecuteOp()
        {
            var searchTerm = new SearchTerm(id: 1);
            return service.ExecuteOp(searchTerm, "get_byId");
        }

        [Benchmark(Description = "Execute with search term")]
        public async Task<int> ExecuteAsync()
        {
            var searchTerm = new SearchTerm(id: 1);
            return await service.ExecuteAsync(searchTerm, "get_byId", CancellationToken.None);
        }

        [Benchmark(Description = "ExecuteOp with search term")]
        public async Task<DbOpAsyncResult> ExecuteOpAsync()
        {
            var searchTerm = new SearchTerm(id: 1);
            return await service.ExecuteOpAsync(searchTerm, "get_byId", CancellationToken.None);
        }

        [Benchmark(Description = "ExecuteOp Resilient error")]
        public DbOpResult ToListError()
        {
            return service.ExecuteOp($"UPDATE {TableName} SET UserName2 = 'sample 2' WHERE Id = 1;", false);
        }

        [Benchmark(Description = "ToSingleOp<> Resilient error")]
        public DbOpResult<Person> ToSingleOpError()
        {
            return service.ToSingleOp<Person>($"SELECT UserName2 FROM {TableName} WHERE Id = 1;", false);
        }

        [Benchmark(Description = "ToListOp<> Resilient error")]
        public DbOpResult<IEnumerable<Person>> ToListOpError()
        {
            return service.ToListOp<Person>($"SELECT UserName2 FROM {TableName} WHERE Id = 1;", false);
        }

        [GlobalCleanup]
        public void CleanTempData()
        {
            Clean();
        }
    }
}
