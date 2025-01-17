using System.Threading;
using TData.Cache;
using TData.Tests.Performance.Entities;

namespace TData.Tests.Performance.Legacy.Tests
{
    public class Single : TestCase
    {
        public Single(string databaseName) : base(databaseName)
        {
        }

        public void Execute(string db, string tableName, int expectedItems = 0)
        {
            var query = $"SELECT TOP 1 UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            PerformOperation(() => DbHub.Use(in db).FetchOne<Person>(query), "FetchOne<>");
            PerformOperation(() => DbHub.Use(in db).TryFetchOne<Person>(query), "TryFetchOne<>");
        }

        public void ExecuteAsync(string db, string tableName, int expectedItems = 0)
        {
            var query = $"SELECT TOP 1 UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            PerformOperationAsync(() => DbHub.Use(in db).FetchOneAsync<Person>(query, null), "FetchOneAsync<>");
            PerformOperationAsync(() => DbHub.Use(in db).TryFetchOneAsync<Person>(query, null), "TryFetchOneAsync<>");
        }

        public void ExecuteCachedDatabase(string db, string tableName, int expectedItems = 0)
        {
            var query = $"SELECT TOP 1 UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            PerformOperation(() => CachedDbHub.Use(in db).FetchOne<Person>(query), "FetchOne<>");
        }
    }
}
