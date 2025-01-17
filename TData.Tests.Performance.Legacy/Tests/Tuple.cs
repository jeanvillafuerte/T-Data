using System.Threading;
using TData.Cache;
using TData.Tests.Performance.Entities;

namespace TData.Tests.Performance.Legacy.Tests
{
    public class Tuple(string databaseName) : TestCase(databaseName)
    {
        public void Execute(string db, string tableName, int expectedItems = 0)
        {
            string query = $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            PerformOperation(() => DbHub.Use(in db).FetchTuple<Person, Person>($"{query}; {query}"), "FetchTuple<>");
            PerformOperation(() => DbHub.Use(in db).TryFetchTuple<Person, Person>($"{query}; {query}"), "TryFetchTuple<>");
        }

        public void ExecuteAsync(string db, string tableName, int expectedItems = 0)
        {
            string query = $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            PerformOperationAsync(() => DbHub.Use(in db).FetchTupleAsync<Person, Person>($"{query}; {query}", null), "FetchTupleAsync<>");
            PerformOperationAsync(() => DbHub.Use(in db).TryFetchTupleAsync<Person, Person>($"{query}; {query}", null), "TryFetchTupleAsync<>");
        }

        public void ExecuteCachedDatabase(string db, string tableName, int expectedItems = 0)
        {
            string query = $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            PerformOperation(() => CachedDbHub.Use(in db).FetchTuple<Person, Person>($"{query}; {query}"), "FetchTuple<>");
            PerformOperation(() => CachedDbHub.Use(in db).FetchTuple<Person, Person, Person>($"{query}; {query}; {query}"), "FetchTuple<>");
        }
    }
}
