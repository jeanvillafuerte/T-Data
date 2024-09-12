using System.Threading;
using System.Threading.Tasks;
using Thomas.Cache;
using Thomas.Database;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Legacy.Tests
{
    public class Single : TestCase
    {
        public Single(string databaseName) : base(databaseName)
        {
        }

        public void Execute(string db, string tableName, int expectedItems = 0)
        {
            var query = $"SELECT TOP 1 UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            PerformOperation(() => DbHub.Use(db).FetchOne<Person>(query), null, "FetchOne<>");
            PerformOperation(() => DbHub.Use(db).TryFetchOne<Person>(query), null, "TryFetchOne<>");
        }

        public async Task ExecuteAsync(string db, string tableName, int expectedItems = 0)
        {
            var query = $"SELECT TOP 1 UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            await PerformOperationAsync(() => DbHub.Use(db).FetchOneAsync<Person>(query, null, CancellationToken.None), null, "FetchOneAsync<>");
            await PerformOperationAsync(() => DbHub.Use(db).TryFetchOneAsync<Person>(query, null, CancellationToken.None), null, "TryFetchOneAsync<>");
        }

        public void ExecuteCachedDatabase(string db, string tableName, int expectedItems = 0)
        {
            var query = $"SELECT TOP 1 UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            PerformOperation(() => CachedDbHub.Use(db).FetchOne<Person>(query), null, "FetchOne<>");
        }
    }
}
