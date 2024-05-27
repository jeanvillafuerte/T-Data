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
            PerformOperation(() => DbFactory.GetDbContext(db).ToSingle<Person>(query), null, "ToSingle<>");
            PerformOperation(() => DbFactory.GetDbContext(db).ToSingleOp<Person>(query), null, "ToSingleOp<>");
        }

        public async Task ExecuteAsync(string db, string tableName, int expectedItems = 0)
        {
            var query = $"SELECT TOP 1 UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            await PerformOperationAsync(() => DbFactory.GetDbContext(db).ToSingleAsync<Person>(query, null, CancellationToken.None), null, "ToSingleAsync<>");
            await PerformOperationAsync(() => DbFactory.GetDbContext(db).ToSingleOpAsync<Person>(query, null, CancellationToken.None), null, "ToSingleOpAsync<>");
        }

        public void ExecuteCachedDatabase(string db, string tableName, int expectedItems = 0)
        {
            var query = $"SELECT TOP 1 UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            PerformOperation(() => CachedDbFactory.GetDbContext(db).ToSingle<Person>(query), null, "ToSingle<>");
        }
    }
}
