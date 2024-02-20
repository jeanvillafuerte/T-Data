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

        public void Execute(IDatabase service, string databaseName, string tableName, int expectedItems = 0)
        {
            var query = $"SELECT TOP 1 UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            PerformOperation(() => service.ToSingle<Person>(query), null, "ToSingle<>");
            PerformOperation(() => service.ToSingleOp<Person>(query), null, "ToSingleOp<>");
        }

        public async Task ExecuteAsync(IDatabase service, string databaseName, string tableName, int expectedItems = 0)
        {
            var query = $"SELECT TOP 1 UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            await PerformOperationAsync(() => service.ToSingleAsync<Person>(query, null, CancellationToken.None), null, "ToSingleAsync<>");
            await PerformOperationAsync(() => service.ToSingleOpAsync<Person>(query, null, CancellationToken.None), null, "ToSingleOpAsync<>");
        }

        public void ExecuteCachedDatabase(ICachedDatabase database, string databaseName, string tableName, int expectedItems = 0)
        {
            var query = $"SELECT TOP 1 UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            PerformOperation(() => database.ToSingle<Person>(query), null, "ToSingle<>");
        }
    }
}
