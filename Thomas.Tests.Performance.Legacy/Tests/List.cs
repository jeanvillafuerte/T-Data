using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Thomas.Cache;
using Thomas.Database;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Legacy.Tests
{
    public class List : TestCase
    {
        public List(string databaseName) : base(databaseName)
        {
        }

        public void Execute(IDatabase service, string tableName, int expectedItems = 0)
        {
            PerformOperation(() => service.ToList<Person>($"SELECT * FROM {tableName} WHERE Id > @Id", new { Id = 0 }), expectedItems, "ToList<>");
            PerformOperation(() => service.ToListOp<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}"), expectedItems, "ToListOp<>");
            PerformOperation(() => service.ToList<Person>($@"get_{tableName}", new { age = 5 }), null, "ToList<> Store Procedure");
            PerformOperation(() => service.ToListOp<Person>($@"get_{tableName}", new { age = 5 }), null, "ToList<> Store Procedure");
            PerformOperation(() =>
            {
                var st = new ListResult(age: 35);
                return service.ToList<Person>("get_byAge", st);
            }, null, "ToList<> by SP");
        }

        public async Task ExecuteAsync(IDatabase service, string tableName, int expectedItems = 0)
        {
            var query = $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            await PerformOperationAsync(() => service.ToListAsync<Person>(query, null, CancellationToken.None), expectedItems: expectedItems, operationName: "ToListAsync<>");
            await PerformOperationAsync(() => service.ToListOpAsync<Person>(query, null, CancellationToken.None), null, "ToListOpAsync<>");
            await PerformOperationAsync(() => service.ToListAsync<Person>($@"get_{tableName}", new { age = 5 }, CancellationToken.None), null, "ToListAsync<> Store Procedure");
            await PerformOperationAsync(() => service.ToListOpAsync<Person>($@"get_{tableName}", new { age = 5 }, CancellationToken.None), null, "ToListOpAsync<> Store Procedure");
            await PerformOperationAsync(() =>
            {
                var st = new ListResult(age: 35);
                return service.ToListOpAsync<Person>("get_byAge", st, CancellationToken.None);
            }, null, "ToListOpAsync<> Store Procedure 2");

            await PerformOperationAsync(() =>
            {
                CancellationTokenSource source = new CancellationTokenSource();
                source.CancelAfter(100);
                return service.ToListOpAsync<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", null, source.Token);
            }, null, "ToListOpAsync<> Cancelled");
        }

        public void ExecuteCachedDatabase(ICachedDatabase database, string tableName, int expectedItems = 0)
        {
            PerformOperation(() => database.ToList<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}"), expectedItems, "ToList<>");
            PerformOperation(() => database.ToList<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", null, refresh: true), expectedItems, "ToList<> (refresh)");
            PerformOperation(() => database.ToList<Person>($@"get_{tableName}", new { age = 5 }), null, "ToList<> By SP");
            PerformOperation(() =>
            {
                var st = new ListResult(age: 35);
                return database.ToList<Person>("get_byAge", st);
            }, null, "ToList<> by SP with output");

            PerformOperation(() =>
            {
                var st = new ListResult(age: 35);
                return database.ToList<Person>("get_byAge", st);
            }, null, "ToList<> (refresh) by SP with output");
        }
    }
}
