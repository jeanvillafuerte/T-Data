using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using BenchmarkDotNet.Attributes;
using Thomas.Database;
using Thomas.Tests.Performance.Entities;
using Thomas.Cache;

namespace Thomas.Tests.Performance.Benchmark
{
    [Description("ThomasDataAdapter")]
#if NETCOREAPP3_0_OR_GREATER
    [ThreadingDiagnoser]
#endif
    [MemoryDiagnoser]
    public class ThomasDataAdapterBenchmark : BenckmarkBase
    {
        [GlobalSetup]
        public void Setup()
        {
            Start();
        }

        [Benchmark(Description = "FetchList<>")]
        public List<Person> FetchList()
        {
            return DbHub.Use("db").FetchList<Person>($"SELECT * FROM {TableName}");
        }

        [Benchmark(Description = "FetchListRecord<>")]
        public List<PersonReadonlyRecord> FetchListRecord()
        {
            return DbHub.Use("db").FetchList<PersonReadonlyRecord>($"SELECT * FROM {TableName}");
        }

        [Benchmark(Description = "FetchList<> Expression")]
        public List<PersonReadonlyRecord> FetchListRecordExpression()
        {
            return DbHub.Use("db").FetchList<PersonReadonlyRecord>(x => x.Id > 0);
        }

        [Benchmark(Description = "FetchList<> (cached)")]
        public List<Person> FetchListCached()
        {
            return CachedDbHub.Use("db").FetchList<Person>($"SELECT * FROM {TableName} WHERE Id > @Id", new { Id = 0 });
        }

        [Benchmark(Description = "FetchListRecord<> Expression")]
        public List<Person> FetchListExpression()
        {
            return DbHub.Use("db").FetchList<Person>(x => x.Id > 0);
        }

        [Benchmark(Description = "FetchList<> Expression")]
        public List<Person> FetchListExpressionCached()
        {
            return CachedDbHub.Use("db").FetchList<Person>(x => x.Id > 1);
        }

        [Benchmark(Description = "FetchList<> (cached & refresh)")]
        public List<Person> FetchListCached2()
        {
            return CachedDbHub.Use("db").FetchList<Person>($"SELECT * FROM {TableName} WHERE Id = @Id", new { Id = 1 }, refresh: true);
        }

        [Benchmark(Description = "FetchOne<>")]
        public Person FetchOne()
        {
            return DbHub.Use("db").FetchOne<Person>($"SELECT * FROM {TableName} WHERE Id = @Id", new { Id = 1 });
        }

        [Benchmark(Description = "FetchListAsync<>")]
        public async Task<List<Person>> FetchListAsync()
        {
            return await DbHub.Use("db").FetchListAsync<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", null, CancellationToken.None);
        }

        [Benchmark(Description = "FetchOneAsync<>")]
        public async Task<Person> FetchOneAsync()
        {
            return await DbHub.Use("db").FetchOneAsync<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", null, CancellationToken.None);
        }

        [Benchmark(Description = "ToTuple<>")]
        public Tuple<List<Person>, List<Person>> ToTuple()
        {
            return DbHub.Use("db").FetchTuple<Person, Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;" + $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "FetchList<> T with nullables")]
        public List<PersonWithNullables> FetchList2()
        {
            return DbHub.Use("db").FetchList<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "FetchOne<> T with nullables")]
        public PersonWithNullables FetchOne2()
        {
            return DbHub.Use("db").FetchOne<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "FetchListAsync<> T with nullables")]
        public async Task<List<PersonWithNullables>> FetchListAsync2()
        {
            return await DbHub.Use("db").FetchListAsync<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", null, CancellationToken.None);
        }

        [Benchmark(Description = "FetchOneAsync<> T with nullables")]
        public async Task<PersonWithNullables> FetchOneAsync2()
        {
            return await DbHub.Use("db").FetchOneAsync<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", null, CancellationToken.None);
        }

        [Benchmark(Description = "ToTuple<> T with nullables")]
        public Tuple<List<PersonWithNullables>, List<PersonWithNullables>> ToTuple2()
        {
            return DbHub.Use("db").FetchTuple<PersonWithNullables, PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;" + $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "TryFetchList<>")]
        public DbOpResult<List<Person>> TryFetchList()
        {
            return DbHub.Use("db").TryFetchList<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "TryFetchOne<>")]
        public DbOpResult<Person> TryFetchOne()
        {
            return DbHub.Use("db").TryFetchOne<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "TryFetchList<>")]
        public async Task<DbOpAsyncResult<List<Person>>> TryFetchListAsync()
        {
            return await DbHub.Use("db").TryFetchListAsync<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", null, CancellationToken.None);
        }

        [Benchmark(Description = "TryFetchOne<>")]
        public async Task<DbOpAsyncResult<Person>> TryFetchOneAsync()
        {
            return await DbHub.Use("db").TryFetchOneAsync<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", null, CancellationToken.None);
        }

        [Benchmark(Description = "TryFetchTuple<>")]
        public DbOpResult<Tuple<List<Person>, List<Person>>> TryFetchTuple()
        {
            return DbHub.Use("db").TryFetchTuple<Person, Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;" + $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "TryFetchList<> T with nullables")]
        public DbOpResult<List<PersonWithNullables>> TryFetchList2OP()
        {
            return DbHub.Use("db").TryFetchList<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "TryFetchList<> T from store procedure")]
        public DbOpResult<List<Person>> TryFetchList3OP()
        {
            return DbHub.Use("db").TryFetchList<Person>("get_persons", new { age = 5 });
        }

        [Benchmark(Description = "TryFetchList<> T with nullables from store procedure")]
        public DbOpResult<List<PersonWithNullables>> TryFetchList4OP()
        {
            return DbHub.Use("db").TryFetchList<PersonWithNullables>("get_persons", new { age = 5 });
        }

        [Benchmark(Description = "TryFetchOne<> T with nullables")]
        public DbOpResult<PersonWithNullables> TryFetchOne()
        {
            return DbHub.Use("db").TryFetchOne<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "TryFetchTuple<> T with nullables")]
        public DbOpResult<Tuple<List<PersonWithNullables>, List<PersonWithNullables>>> ToTuple2Op()
        {
            return DbHub.Use("db").TryFetchTuple<PersonWithNullables, PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;" + $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "Execute with search term")]
        public int Execute()
        {
            var searchTerm = new SearchTerm(id: 1);
            return DbHub.Use("db").Execute("get_byId", searchTerm);
        }

        [Benchmark(Description = "ExecuteOp with search term")]
        public DbOpResult ExecuteOp()
        {
            var searchTerm = new SearchTerm(id: 1);
            return DbHub.Use("db").TryExecute("get_byId", searchTerm);
        }

        [Benchmark(Description = "Execute with search term (async)")]
        public async Task<int> ExecuteAsync()
        {
            var searchTerm = new SearchTerm(id: 1);
            return await DbHub.Use("db").ExecuteAsync("get_byId", searchTerm, CancellationToken.None);
        }

        [Benchmark(Description = "ExecuteOp with search term (async)")]
        public async Task<DbOpAsyncResult> ExecuteOpAsync()
        {
            var searchTerm = new SearchTerm(id: 1);
            return await DbHub.Use("db").TryExecuteAsync("get_byId", searchTerm, CancellationToken.None);
        }

        [Benchmark(Description = "ExecuteOp Resilient error")]
        public DbOpResult FetchListError()
        {
            return DbHub.Use("db").TryExecute($"UPDATE {TableName} SET UserName2 = 'sample 2' WHERE Id = 1;");
        }

        [Benchmark(Description = "TryFetchOne<> Resilient error")]
        public DbOpResult<Person> TryFetchOneError()
        {
            return DbHub.Use("db").TryFetchOne<Person>($"SELECT UserName2 FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "TryFetchList<> Resilient error")]
        public DbOpResult<List<Person>> TryFetchListError()
        {
            return DbHub.Use("db").TryFetchList<Person>($"SELECT UserName2 FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "Transaction 1")]
        public List<Person> Transaction1()
        {
            return DbHub.Use("db").ExecuteTransaction((db) =>
            {
                db.Execute($"UPDATE {TableName} SET UserName = 'NEW_NAME' WHERE Id = @Id", new { Id = 1 });
                db.Execute($"UPDATE {TableName} SET UserName = 'NEW_NAME_2' WHERE Id = @Id", new { Id = 1 });
                return db.FetchList<Person>($"SELECT * FROM {TableName}");
            });
        }

        [Benchmark(Description = "Transaction 2")]
        public bool Transaction2()
        {
            return DbHub.Use("db").ExecuteTransaction((db) =>
            {
                var searchTerm = new SearchTerm(id: 1);
                db.Execute($"UPDATE {TableName} SET UserName = 'NEW_NAME' WHERE Id = @Id", new { Id = 1 });
                db.Execute("get_byId", searchTerm);

                if (searchTerm.UserName.IsNullOrEmpty())
                    return db.Rollback();
                else
                    return db.Commit();
            });
        }

        [Benchmark(Description = "Transaction Rollback")]
        public bool Transaction3()
        {
            return DbHub.Use("db").ExecuteTransaction((db) =>
             {
                 db.Execute($"UPDATE {TableName} SET UserName = 'NEW_NAME' WHERE Id = @Id", new { Id = 1 });
                 db.Execute($"UPDATE {TableName} SET UserName = 'NEW_NAME_2' WHERE Id = @Id", new { Id = 1 });
                 return db.Rollback();
             });
        }

        [GlobalCleanup]
        public void CleanTempData()
        {
            Clean();
        }
    }
}
