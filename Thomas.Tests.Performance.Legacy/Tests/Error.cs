using System.Threading;
using System.Threading.Tasks;
using Thomas.Cache;
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
            PerformOperation(() => DbFactory.GetDbContext(db).ToListOp<Person>($@"SELECT UserName2 FROM {tableName}"), null, "ToListOp<> error resilient");
            PerformOperation(() => DbFactory.GetDbContext(db).ToSingleOp<Person>($@"SELECT UserName2 FROM {tableName}"), null, "ToSingleOp<> error resilient");
        }

        public async Task ExecuteAsync(string db, string tableName, int expectedItems = 0)
        {
            await PerformOperationAsync(() => DbFactory.GetDbContext(db).ToListOpAsync<Person>($@"SELECT UserName2 FROM {tableName}", null, CancellationToken.None), null, "ToListOpAsync<> error resilient");
            await PerformOperationAsync(() => DbFactory.GetDbContext(db).ToSingleOpAsync<Person>($@"SELECT UserName2 FROM {tableName}", null, CancellationToken.None), null, "ToSingleOpAsync<> error resilient");
        }

        public void ExecuteCachedDatabase(string db, string tableName, int expectedItems = 0)
        {

        }
    }
}
