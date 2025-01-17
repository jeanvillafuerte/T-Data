using System.Threading;
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
            PerformOperation(() => DbHub.Use(in db).TryFetchList<Person>($@"SELECT UserName2 FROM {tableName}"), "TryFetchList<> (Error)");
            PerformOperation(() => DbHub.Use(in db).TryFetchOne<Person>($@"SELECT UserName2 FROM {tableName}"), "TryFetchOne<> (Error)");
        }

        public void ExecuteAsync(string db, string tableName, int expectedItems = 0)
        {
            PerformOperationAsync(() => DbHub.Use(in db).TryFetchListAsync<Person>($@"SELECT UserName2 FROM {tableName}", null, CancellationToken.None), "TryFetchListAsync<> (Error)", true);
            PerformOperationAsync(() => DbHub.Use(in db).TryFetchOneAsync<Person>($@"SELECT UserName2 FROM {tableName}", null, CancellationToken.None), "TryFetchOneAsync<> (Error)", true);
        }

        public void ExecuteCachedDatabase(string db, string tableName, int expectedItems = 0)
        {

        }
    }
}
