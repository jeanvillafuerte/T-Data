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
            PerformOperation(() => DbFactory.GetDbContext(db).ToTuple<Person, Person>($"{query}; {query}"), null, "ToTuple<>");
            PerformOperation(() => DbFactory.GetDbContext(db).ToTupleOp<Person, Person>($"{query}; {query}"), null, "ToTupleOp<>");
        }

        public async Task ExecuteAsync(string db, string tableName, int expectedItems = 0)
        {
            string query = $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            await PerformOperationAsync(() => DbFactory.GetDbContext(db).ToTupleAsync<Person, Person>($"{query}; {query}", null, CancellationToken.None), null, "ToTupleAsync<>");
            await PerformOperationAsync(() => DbFactory.GetDbContext(db).ToTupleOpAsync<Person, Person>($"{query}; {query}", null, CancellationToken.None), null, "ToTupleOpAsync<>");
        }

        public void ExecuteCachedDatabase(string db, string tableName, int expectedItems = 0)
        {
            string query = $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            PerformOperation(() => CachedDbFactory.GetDbContext(db).ToTuple<Person, Person>($"{query}; {query}"), null, "ToTuple<>");
            PerformOperation(() => CachedDbFactory.GetDbContext(db).ToTuple<Person, Person, Person>($"{query}; {query}; {query}"), null, "ToTuple<>");
        }
    }
}
