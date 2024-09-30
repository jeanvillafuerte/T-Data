using System.Threading;
using TData.Cache;
using TData;
using TData.Tests.Performance.Entities;

namespace TData.Tests.Performance.Legacy.Tests
{
    public class Tuple(string databaseName) : TestCase(databaseName)
    {
        public void Execute(string db, string tableName, int expectedItems = 0)
        {
            string query = $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            PerformOperation(() => DbHub.Use(db).FetchTuple<Person, Person>($"{query}; {query}"), "FetchTuple<>");
            PerformOperation(() => DbHub.Use(db).TryFetchTuple<Person, Person>($"{query}; {query}"), "TryFetchTuple<>");
        }

        public void ExecuteAsync(string db, string tableName, int expectedItems = 0)
        {
            string query = $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            PerformOperationAsync(() => DbHub.Use(db).FetchTupleAsync<Person, Person>($"{query}; {query}", null, CancellationToken.None), "FetchTupleAsync<>");
            PerformOperationAsync(() => DbHub.Use(db).TryFetchTupleAsync<Person, Person>($"{query}; {query}", null, CancellationToken.None), "TryFetchTupleAsync<>");
        }

        public void ExecuteCachedDatabase(string db, string tableName, int expectedItems = 0)
        {
            string query = $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            PerformOperation(() => CachedDbHub.Use(db).FetchTuple<Person, Person>($"{query}; {query}"), "FetchTuple<>");
            PerformOperation(() => CachedDbHub.Use(db).FetchTuple<Person, Person, Person>($"{query}; {query}; {query}"), "FetchTuple<>");
        }
    }
}
