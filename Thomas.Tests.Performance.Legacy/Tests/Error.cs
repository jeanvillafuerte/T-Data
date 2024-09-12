using System.Threading;
using System.Threading.Tasks;
using Thomas.Database;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Legacy.Tests
{
    public class Error : TestCase
    {
        public Error(string databaseName) : base(databaseName)
        {
        }

        public void Execute(string db, string tableName, int expectedItems = 0)
        {
            PerformOperation(() => DbHub.Use(db).TryFetchList<Person>($@"SELECT UserName2 FROM {tableName}"), null, "TryFetchList<>");
            PerformOperation(() => DbHub.Use(db).TryFetchOne<Person>($@"SELECT UserName2 FROM {tableName}"), null, "TryFetchOne<>");
        }

        public async Task ExecuteAsync(string db, string tableName, int expectedItems = 0)
        {
            await PerformOperationAsync(() => DbHub.Use(db).TryFetchListAsync<Person>($@"SELECT UserName2 FROM {tableName}", null, CancellationToken.None), null, "TryFetchListAsync<>", true);
            await PerformOperationAsync(() => DbHub.Use(db).TryFetchOneAsync<Person>($@"SELECT UserName2 FROM {tableName}", null, CancellationToken.None), null, "TryFetchOneAsync<>", true);
        }

        public void ExecuteCachedDatabase(string db, string tableName, int expectedItems = 0)
        {

        }
    }
}
