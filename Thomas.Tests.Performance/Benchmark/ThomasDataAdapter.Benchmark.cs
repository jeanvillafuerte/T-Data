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

namespace Thomas.Tests.Performance.Benchmark
{
    [Description("ThomasDataAdapter")]
    [ThreadingDiagnoser]
    [MemoryDiagnoser]
    public class ThomasDataAdapterBenckmark : BenckmarkBase
    {
        private readonly Consumer consumer = new Consumer();

        [GlobalSetup]
        public void Setup()
        {
            Start();
        }

        [Benchmark(Description = "ToList<> (unbuffered)")]
        public void NoPreparedToList()
        {
            Database.ToList<Person>($"SELECT * FROM {TableName} WHERE Id = @Id", new { Id = 1 }).Consume(consumer);
        }

        [Benchmark(Description = "ToList<> (buffered)")]
        public void ToListCached()
        {
            var list = Database2.ToList<Person>($"SELECT * FROM {TableName} WHERE Id = @Id", new { Id = 1 });
            list.Consume(consumer);
        }

        [Benchmark(Description = "ToList<> Expression (unbuffered)")]
        public void ToListExpression()
        {
            Database.ToList<Person>(x => x.Id == 1).Consume(consumer);
        }

        [Benchmark(Description = "ToList<> Expression (buffered)")]
        public void ToListExpression2()
        {
            var list = Database2.ToList<Person>(x => x.Id == 1);
            list.Consume(consumer);
        }

        [Benchmark(Description = "ToList<> (buffered & refresh)")]
        public void ToListCached2()
        {
            var list = Database2.ToList<Person>($"SELECT * FROM {TableName} WHERE Id = @Id", new { Id = 1 }, refresh: true);
            list.Consume(consumer);
        }

        [Benchmark(Description = "Single<>")]
        public Person Single()
        {
            return Database.ToSingle<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "ToListAsync<>")]
        public async Task ToListAsync()
        {
            var result = await Database.ToListAsync<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", null, CancellationToken.None);
            result.Consume(consumer);
        }

        [Benchmark(Description = "SingleAsync<>")]
        public async Task<Person> SingleAsync()
        {
            return await Database.ToSingleAsync<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", null, CancellationToken.None);
        }

        [Benchmark(Description = "ToTuple<>")]
        public Tuple<List<Person>, List<Person>> ToTuple()
        {
            return Database.ToTuple<Person, Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;" + $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "ToList<> T with nullables")]
        public void ToList2()
        {
            var result = Database.ToList<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
            result.Consume(consumer);
        }

        [Benchmark(Description = "Single<> T with nullables")]
        public PersonWithNullables Single2()
        {
            return Database.ToSingle<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "ToListAsync<> T with nullables")]
        public async Task ToListAsync2()
        {
            var result = await Database.ToListAsync<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", null, CancellationToken.None);
            result.Consume(consumer);
        }

        [Benchmark(Description = "SingleAsync<> T with nullables")]
        public async Task<PersonWithNullables> SingleAsync2()
        {
            return await Database.ToSingleAsync<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", null, CancellationToken.None);
        }

        [Benchmark(Description = "ToTuple<> T with nullables")]
        public Tuple<List<PersonWithNullables>, List<PersonWithNullables>> ToTuple2()
        {
            return Database.ToTuple<PersonWithNullables, PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;" + $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "ToListOp<>")]
        public DbOpResult<List<Person>> ToListOp()
        {
            return Database.ToListOp<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "SingleOp<>")]
        public DbOpResult<Person> SingleOp()
        {
            return Database.ToSingleOp<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "ToListOp<>")]
        public async Task<DbOpAsyncResult<List<Person>>> ToListOpAsync()
        {
            return await Database.ToListOpAsync<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", null, CancellationToken.None);
        }

        [Benchmark(Description = "SingleOp<>")]
        public async Task<DbOpAsyncResult<Person>> SingleOpAsync()
        {
            return await Database.ToSingleOpAsync<Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;", null, CancellationToken.None);
        }

        [Benchmark(Description = "ToTupleOp<>")]
        public DbOpResult<Tuple<List<Person>, List<Person>>> ToTupleOp()
        {
            return Database.ToTupleOp<Person, Person>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;" + $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "ToListOp<> T with nullables")]
        public DbOpResult<List<PersonWithNullables>> ToList2op()
        {
            return Database.ToListOp<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "ToListOp<> T from store procedure")]
        public DbOpResult<List<Person>> ToList3op()
        {
            return Database.ToListOp<Person>("get_persons", new { age = 5 });
        }

        [Benchmark(Description = "ToListOp<> T with nullables from store procedure")]
        public DbOpResult<List<PersonWithNullables>> ToList4op()
        {
            return Database.ToListOp<PersonWithNullables>("get_persons", new { age = 5 });
        }

        [Benchmark(Description = "SingleOp<> T with nullables")]
        public DbOpResult<PersonWithNullables> Single2Op()
        {
            return Database.ToSingleOp<PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "ToTupleOp<> T with nullables")]
        public DbOpResult<Tuple<List<PersonWithNullables>, List<PersonWithNullables>>> ToTuple2Op()
        {
            return Database.ToTupleOp<PersonWithNullables, PersonWithNullables>($"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;" + $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "Execute with search term")]
        public int Execute()
        {
            var searchTerm = new SearchTerm(id: 1);
            return Database.Execute("get_byId", searchTerm);
        }

        [Benchmark(Description = "ExecuteOp with search term")]
        public DbOpResult ExecuteOp()
        {
            var searchTerm = new SearchTerm(id: 1);
            return Database.ExecuteOp("get_byId", searchTerm);
        }

        [Benchmark(Description = "Execute with search term (async)")]
        public async Task<int> ExecuteAsync()
        {
            var searchTerm = new SearchTerm(id: 1);
            return await Database.ExecuteAsync("get_byId", searchTerm, CancellationToken.None);
        }

        [Benchmark(Description = "ExecuteOp with search term (async)")]
        public async Task<DbOpAsyncResult> ExecuteOpAsync()
        {
            var searchTerm = new SearchTerm(id: 1);
            return await Database.ExecuteOpAsync("get_byId", searchTerm, CancellationToken.None);
        }

        [Benchmark(Description = "ExecuteOp Resilient error")]
        public DbOpResult ToListError()
        {
            return Database.ExecuteOp($"UPDATE {TableName} SET UserName2 = 'sample 2' WHERE Id = 1;");
        }

        [Benchmark(Description = "ToSingleOp<> Resilient error")]
        public DbOpResult<Person> ToSingleOpError()
        {
            return Database.ToSingleOp<Person>($"SELECT UserName2 FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "ToListOp<> Resilient error")]
        public DbOpResult<List<Person>> ToListOpError()
        {
            return Database.ToListOp<Person>($"SELECT UserName2 FROM {TableName} WHERE Id = 1;");
        }

        [Benchmark(Description = "Transaction 1")]
        public void Transaction1()
        {
            var list = Database.ExecuteTransaction((db) =>
            {
                db.Execute($"UPDATE {TableName} SET UserName = 'NEW_NAME' WHERE Id = @Id", new { Id = 1 });
                db.Execute($"UPDATE {TableName} SET UserName = 'NEW_NAME_2' WHERE Id = @Id", new { Id = 1 });
                return db.ToList<Person>($"SELECT * FROM {TableName}");
            });

            list.Consume(consumer);
        }

        [Benchmark(Description = "Transaction 2")]
        public bool Transaction2()
        {
            var searchTerm = new SearchTerm(id: 1);
            return Database.ExecuteTransaction((db) =>
            {
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
            return Database.ExecuteTransaction((db) =>
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
