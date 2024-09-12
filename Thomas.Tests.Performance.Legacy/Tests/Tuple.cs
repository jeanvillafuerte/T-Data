using System.Threading;
using System.Threading.Tasks;
using Thomas.Cache;
using Thomas.Database;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Legacy.Tests
{
    public class Tuple(string databaseName) : TestCase(databaseName)
    {
        public void Execute(string db, string tableName, int expectedItems = 0)
        {
            string query = $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            PerformOperation(() => DbHub.Use(db).FetchTuple<Person, Person>($"{query}; {query}"), null, "FetchTuple<>");
            PerformOperation(() => DbHub.Use(db).TryFetchTuple<Person, Person>($"{query}; {query}"), null, "TryFetchTuple<>");
        }

        public async Task ExecuteAsync(string db, string tableName, int expectedItems = 0)
        {
            string query = $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            await PerformOperationAsync(() => DbHub.Use(db).FetchTupleAsync<Person, Person>($"{query}; {query}", null, CancellationToken.None), null, "FetchTupleAsync<>");
            await PerformOperationAsync(() => DbHub.Use(db).TryFetchTupleAsync<Person, Person>($"{query}; {query}", null, CancellationToken.None), null, "TryFetchTupleAsync<>");
        }

        public void ExecuteCachedDatabase(string db, string tableName, int expectedItems = 0)
        {
            string query = $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            PerformOperation(() => CachedDbHub.Use(db).FetchTuple<Person, Person>($"{query}; {query}"), null, "FetchTuple<>");
            PerformOperation(() => CachedDbHub.Use(db).FetchTuple<Person, Person, Person>($"{query}; {query}; {query}"), null, "FetchTuple<>");
        }
    }
}
