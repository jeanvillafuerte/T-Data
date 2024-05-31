using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
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
    public class ThomasDataAdapterBenckmark : BenckmarkBase
    {
        [GlobalSetup]
        public void Setup()
        {
            Start();
        }

        [Benchmark(Description = "ToList<>", Baseline = true)]
        public List<Person> ToList()
        {
            return DbFactory.GetDbContext("db").ToList<Person>($"SELECT * FROM {TableName}");
        }

        [Benchmark(Description = "ToListRecord<>")]
        public List<PersonReadonlyRecord> ToListRecord()
        {
            return DbFactory.GetDbContext("db").ToList<PersonReadonlyRecord>($"SELECT * FROM {TableName}");
        }

        [Benchmark(Description = "ToList<> Expression")]
        public List<PersonReadonlyRecord> ToListRecordExpression()
        {
            return DbFactory.GetDbContext("db").ToList<PersonReadonlyRecord>(x => x.Id > 0);
        }

        [Benchmark(Description = "ToList<> (cached)")]
        public List<Person> ToListCached()
        {
           return CachedDbFactory.GetDbContext("db").ToList<Person>($"SELECT * FROM {TableName} WHERE Id > @Id", new { Id = 0 });
        }

        [Benchmark(Description = "ToListRecord<> Expression")]
        public List<Person> ToListExpression()
        {
           return DbFactory.GetDbContext("db").ToList<Person>(x => x.Id > 0);
        }

        [Benchmark(Description = "ToList<> Expression")]
        public List<Person> ToListExpressionCached()
        {
           return CachedDbFactory.GetDbContext("db").ToList<Person>(x => x.Id > 1);
        }

        [Benchmark(Description = "ToList<> (cached & refresh)")]
        public List<Person> ToListCached2()
        {
           return CachedDbFactory.GetDbContext("db").ToList<Person>($"SELECT * FROM {TableName} WHERE Id = @Id", new { Id = 1 }, refresh: true);
        }

        [Benchmark(Description = "Single<>")]
        public Person Single()
        {
            return DbFactory.GetDbContext("db").ToSingle<Person>($"SELECT * FROM {TableName} WHERE Id = @Id", new { Id = 1 });
        }

        [Benchmark(Description = "ToListAsync<>")]
        public async Task<List<Person>> ToListAsync()
        {
           return await DbFactory.GetDbContext("db").ToListAsync<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", null, CancellationToken.None);
        }

        [Benchmark(Description = "SingleAsync<>")]
        public async Task<Person> SingleAsync()
        {
           return await DbFactory.GetDbContext("db").ToSingleAsync<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", null, CancellationToken.None);
        }

        [Benchmark(Description = "ToTuple<>")]
        public Tuple<List<Person>, List<Person>> ToTuple()
        {
           return DbFactory.GetDbContext("db").ToTuple<Person, Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;" + $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "ToList<> T with nullables")]
        public List<PersonWithNullables> ToList2()
        {
           return DbFactory.GetDbContext("db").ToList<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "Single<> T with nullables")]
        public PersonWithNullables Single2()
        {
           return DbFactory.GetDbContext("db").ToSingle<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "ToListAsync<> T with nullables")]
        public async Task<List<PersonWithNullables>> ToListAsync2()
        {
           return await DbFactory.GetDbContext("db").ToListAsync<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", null, CancellationToken.None);
        }

        [Benchmark(Description = "SingleAsync<> T with nullables")]
        public async Task<PersonWithNullables> SingleAsync2()
        {
           return await DbFactory.GetDbContext("db").ToSingleAsync<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", null, CancellationToken.None);
        }

        [Benchmark(Description = "ToTuple<> T with nullables")]
        public Tuple<List<PersonWithNullables>, List<PersonWithNullables>> ToTuple2()
        {
           return DbFactory.GetDbContext("db").ToTuple<PersonWithNullables, PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;" + $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "ToListOp<>")]
        public DbOpResult<List<Person>> ToListOp()
        {
           return DbFactory.GetDbContext("db").ToListOp<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "SingleOp<>")]
        public DbOpResult<Person> SingleOp()
        {
           return DbFactory.GetDbContext("db").ToSingleOp<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "ToListOp<>")]
        public async Task<DbOpAsyncResult<List<Person>>> ToListOpAsync()
        {
           return await DbFactory.GetDbContext("db").ToListOpAsync<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", null, CancellationToken.None);
        }

        [Benchmark(Description = "SingleOp<>")]
        public async Task<DbOpAsyncResult<Person>> SingleOpAsync()
        {
           return await DbFactory.GetDbContext("db").ToSingleOpAsync<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", null, CancellationToken.None);
        }

        [Benchmark(Description = "ToTupleOp<>")]
        public DbOpResult<Tuple<List<Person>, List<Person>>> ToTupleOp()
        {
           return DbFactory.GetDbContext("db").ToTupleOp<Person, Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;" + $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "ToListOp<> T with nullables")]
        public DbOpResult<List<PersonWithNullables>> ToList2op()
        {
           return DbFactory.GetDbContext("db").ToListOp<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "ToListOp<> T from store procedure")]
        public DbOpResult<List<Person>> ToList3op()
        {
           return DbFactory.GetDbContext("db").ToListOp<Person>("get_persons", new { age = 5 });
        }

        [Benchmark(Description = "ToListOp<> T with nullables from store procedure")]
        public DbOpResult<List<PersonWithNullables>> ToList4op()
        {
           return DbFactory.GetDbContext("db").ToListOp<PersonWithNullables>("get_persons", new { age = 5 });
        }

        [Benchmark(Description = "SingleOp<> T with nullables")]
        public DbOpResult<PersonWithNullables> Single2Op()
        {
           return DbFactory.GetDbContext("db").ToSingleOp<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "ToTupleOp<> T with nullables")]
        public DbOpResult<Tuple<List<PersonWithNullables>, List<PersonWithNullables>>> ToTuple2Op()
        {
           return DbFactory.GetDbContext("db").ToTupleOp<PersonWithNullables, PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;" + $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "Execute with search term")]
        public int Execute()
        {
           var searchTerm = new SearchTerm(id: 1);
           return DbFactory.GetDbContext("db").Execute("get_byId", searchTerm);
        }

        [Benchmark(Description = "ExecuteOp with search term")]
        public DbOpResult ExecuteOp()
        {
           var searchTerm = new SearchTerm(id: 1);
           return DbFactory.GetDbContext("db").ExecuteOp("get_byId", searchTerm);
        }

        [Benchmark(Description = "Execute with search term (async)")]
        public async Task<int> ExecuteAsync()
        {
           var searchTerm = new SearchTerm(id: 1);
           return await DbFactory.GetDbContext("db").ExecuteAsync("get_byId", searchTerm, CancellationToken.None);
        }

        [Benchmark(Description = "ExecuteOp with search term (async)")]
        public async Task<DbOpAsyncResult> ExecuteOpAsync()
        {
           var searchTerm = new SearchTerm(id: 1);
           return await DbFactory.GetDbContext("db").ExecuteOpAsync("get_byId", searchTerm, CancellationToken.None);
        }

        [Benchmark(Description = "ExecuteOp Resilient error")]
        public DbOpResult ToListError()
        {
           return DbFactory.GetDbContext("db").ExecuteOp($"UPDATE {TableName} SET UserName2 = 'sample 2' WHERE Id = 1;");
        }

        [Benchmark(Description = "ToSingleOp<> Resilient error")]
        public DbOpResult<Person> ToSingleOpError()
        {
           return DbFactory.GetDbContext("db").ToSingleOp<Person>($"SELECT UserName2 FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "ToListOp<> Resilient error")]
        public DbOpResult<List<Person>> ToListOpError()
        {
           return DbFactory.GetDbContext("db").ToListOp<Person>($"SELECT UserName2 FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "Transaction 1")]
        public List<Person> Transaction1()
        {
           return DbFactory.GetDbContext("db").ExecuteTransaction((db) =>
           {
               db.Execute($"UPDATE {TableName} SET UserName = 'NEW_NAME' WHERE Id = @Id", new { Id = 1 });
               db.Execute($"UPDATE {TableName} SET UserName = 'NEW_NAME_2' WHERE Id = @Id", new { Id = 1 });
               return db.ToList<Person>($"SELECT * FROM {TableName}");
           });
        }

        [Benchmark(Description = "Transaction 2")]
        public bool Transaction2()
        {
           return DbFactory.GetDbContext("db").ExecuteTransaction((db) =>
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
           return DbFactory.GetDbContext("db").ExecuteTransaction((db) =>
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
