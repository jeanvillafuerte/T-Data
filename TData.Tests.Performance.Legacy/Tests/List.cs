using System.Threading;
using System.Threading.Tasks;
using TData.Cache;
using TData;
using TData.Tests.Performance.Entities;

namespace TData.Tests.Performance.Legacy.Tests
{
    public class List : TestCase
    {
        public List(string databaseName) : base(databaseName)
        {
        }

        public void Execute(string db, string tableName, int expectedItems = 0)
        {
            PerformOperation(() => DbHub.Use(db).FetchList<Person>($"SELECT * FROM {tableName} WHERE Id > @Id", new { Id = 0 }), expectedItems, "FetchList<>");
            PerformOperation(() => DbHub.Use(db).TryFetchList<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}"), expectedItems, "TryFetchList<>");
            PerformOperation(() => DbHub.Use(db).FetchList<Person>($@"get_{tableName}", new { age = 5 }), null, "FetchList<> Store Procedure");
            PerformOperation(() => DbHub.Use(db).TryFetchList<Person>($@"get_{tableName}", new { age = 5 }), null, "TryFetchList<> Store Procedure");
            PerformOperation(() =>
            {
                var st = new ListResult(age: 35);
                return DbHub.Use(db).FetchList<Person>("get_byAge", st);
            }, null, "FetchList<> by SP");
        }

        public void ExecuteAsync(string db, string tableName, int expectedItems = 0)
        {
            var query = $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            PerformOperationAsync(() => DbHub.Use(db).FetchListAsync<Person>(query, null, CancellationToken.None), expectedItems: expectedItems, operationName: "FetchListAsync<>");
            PerformOperationAsync(() => DbHub.Use(db).TryFetchListAsync<Person>(query, null, CancellationToken.None), "TryFetchListAsync<>");
            PerformOperationAsync(() => DbHub.Use(db).FetchListAsync<Person>($@"get_{tableName}", new { age = 5 }, CancellationToken.None), "FetchListAsync<> Store Procedure");
            PerformOperationAsync(() => DbHub.Use(db).TryFetchListAsync<Person>($@"get_{tableName}", new { age = 5 }, CancellationToken.None), "TryFetchListAsync<> Store Procedure");
            PerformOperationAsync(() =>
            {
                var st = new ListResult(age: 35);
                return DbHub.Use(db).TryFetchListAsync<Person>("get_byAge", st, CancellationToken.None);
            }, "TryFetchListAsync<> Store Procedure 2");

            PerformOperationAsync(() =>
            {
                CancellationTokenSource source = new CancellationTokenSource();
                source.CancelAfter(100);
                return DbHub.Use(db).TryFetchListAsync<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", null, source.Token);
            }, "TryFetchListAsync<> Cancelled", true);
        }

        public void ExecuteCachedDatabase(string db, string tableName, int expectedItems = 0)
        {
            PerformOperation(() => CachedDbHub.Use(db).FetchList<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}"), expectedItems, "FetchList<>");
            PerformOperation(() => CachedDbHub.Use(db).FetchList<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", null, refresh: true), expectedItems, "FetchList<> (refresh)");
            PerformOperation(() => CachedDbHub.Use(db).FetchList<Person>($@"get_{tableName}", new { age = 5 }), null, "FetchList<> By SP");
            PerformOperation(() =>
            {
                var st = new ListResult(age: 35);
                return CachedDbHub.Use(db).FetchList<Person>("get_byAge", st);
            }, null, "FetchList<> by SP with output");

            PerformOperation(() =>
            {
                var st = new ListResult(age: 35);
                return CachedDbHub.Use(db).FetchList<Person>("get_byAge", st);
            }, null, "FetchList<> (refresh) by SP with output");
        }
    }
}
