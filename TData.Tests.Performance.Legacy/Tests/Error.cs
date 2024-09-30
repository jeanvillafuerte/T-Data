using System.Threading;
using System.Threading.Tasks;
using TData;
using TData.Tests.Performance.Entities;

namespace TData.Tests.Performance.Legacy.Tests
{
    public class Error : TestCase
    {
        public Error(string databaseName) : base(databaseName)
        {
        }

        public void Execute(string db, string tableName, int expectedItems = 0)
        {
            PerformOperation(() => DbHub.Use(db).TryFetchList<Person>($@"SELECT UserName2 FROM {tableName}"), "TryFetchList<> (Error)");
            PerformOperation(() => DbHub.Use(db).TryFetchOne<Person>($@"SELECT UserName2 FROM {tableName}"), "TryFetchOne<> (Error)");
        }

        public void ExecuteAsync(string db, string tableName, int expectedItems = 0)
        {
            PerformOperationAsync(() => DbHub.Use(db).TryFetchListAsync<Person>($@"SELECT UserName2 FROM {tableName}", null, CancellationToken.None), "TryFetchListAsync<> (Error)", true);
            PerformOperationAsync(() => DbHub.Use(db).TryFetchOneAsync<Person>($@"SELECT UserName2 FROM {tableName}", null, CancellationToken.None), "TryFetchOneAsync<> (Error)", true);
        }

        public void ExecuteCachedDatabase(string db, string tableName, int expectedItems = 0)
        {

        }
    }
}
