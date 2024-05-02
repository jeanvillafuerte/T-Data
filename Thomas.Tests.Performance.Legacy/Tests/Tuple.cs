using System.Threading;
using System.Threading.Tasks;
using Thomas.Cache;
using Thomas.Database;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Legacy.Tests
{
    public class Tuple(string databaseName) : TestCase(databaseName)
    {
        public void Execute(IDatabase service, string tableName, int expectedItems = 0)
        {
            string query = $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            PerformOperation(() => service.ToTuple<Person, Person>($"{query}; {query}"), null, "ToTuple<>");
            PerformOperation(() => service.ToTupleOp<Person, Person>($"{query}; {query}"), null, "ToTupleOp<>");
        }

        public async Task ExecuteAsync(IDatabase service, string tableName, int expectedItems = 0)
        {
            string query = $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            await PerformOperationAsync(() => service.ToTupleAsync<Person, Person>($"{query}; {query}", null, CancellationToken.None), null, "ToTupleAsync<>");
            await PerformOperationAsync(() => service.ToTupleOpAsync<Person, Person>($"{query}; {query}", null, CancellationToken.None), null, "ToTupleOpAsync<>");
        }

        public void ExecuteCachedDatabase(ICachedDatabase database, string tableName, int expectedItems = 0)
        {
            string query = $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            PerformOperation(() => database.ToTuple<Person, Person>($"{query}; {query}"), null, "ToTuple<>");
            PerformOperation(() => database.ToTuple<Person, Person, Person>($"{query}; {query}; {query}"), null, "ToTuple<>");
        }
    }
}
