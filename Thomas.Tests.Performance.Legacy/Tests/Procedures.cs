using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Thomas.Cache;
using Thomas.Database;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Legacy.Tests
{
    public class Procedures : TestCase
    {

        public Procedures(string databaseName) : base(databaseName)
        {
        }

        public void Execute(IDatabase service, string databaseName, string tableName, int expectedItems = 0)
        {
            PerformOperation(() =>
            {
                var st = new SearchTerm(id: 1);
                return service.ExecuteOp("get_byid", st);
            }, null, "ExecuteOp");

            PerformOperation(() =>
            {
                var st = new ListResult(age: 35);
                return service.ToListOp<Person>("get_byAge", st);
            }, null, "ToListOp");

            PerformOperation(() =>
            {
                return DbFactory.CreateDbContext("db1").ExecuteTransaction((db) =>
                {
                    var data = db.ToList<Person>($"SELECT * FROM {tableName}").ToArray();
                    db.Execute($"UPDATE {tableName} SET UserName = 'NEW_NAME' WHERE Id = @Id", new { data[0].Id });
                    db.Execute($"UPDATE {tableName} SET UserName = 'NEW_NAME_2' WHERE Id = @Id", new { data[1].Id });

                    return db.ToList<Person>($"SELECT * FROM {tableName}");
                });
                
            }, null, "Transaction");

            PerformOperation(() =>
            {
                return DbFactory.CreateDbContext("db1").ExecuteTransaction((db) =>
                {
                    db.Execute($"UPDATE {tableName} SET UserName = 'NEW_NAME_3' WHERE Id = @Id", new { Id = 1 });
                    db.Execute($"UPDATE {tableName} SET UserName = 'NEW_NAME_4' WHERE Id = @Id", new { Id = 2 });
                    return db.Rollback();
                });

            }, null, "Transaction Rollback");
        }

        public async Task ExecuteAsync(IDatabase service, string databaseName, string tableName, int expectedItems = 0)
        {
            await PerformOperationAsync(() =>
            {
                CancellationTokenSource source = new CancellationTokenSource();
                source.CancelAfter(1);
                try
                {
                    service.ExecuteAsync("WAITFOR DELAY '00:00:10'", false, source.Token);
                }
                catch (System.Exception ex) { }

                return service.ExecuteAsync("SELECT 1", false, CancellationToken.None);
            }, null, "ExecuteAsync Timeout");

            await PerformOperationAsync(() =>
            {
                CancellationTokenSource source = new CancellationTokenSource();
                source.CancelAfter(1);
                return service.ExecuteOpAsync("WAITFOR DELAY '00:00:10'", false, source.Token);
            }, null, "ExecuteOpAsync Timeout");

            await PerformOperationAsync(() =>
            {
                return DbFactory.CreateDbContext("db2").ExecuteTransactionAsync(async (db) =>
                {
                    await db.ExecuteAsync($"UPDATE {tableName} SET UserName = 'NEW_NAME' WHERE Id = @Id", new { Id = 1 }, CancellationToken.None);
                    await db.ExecuteAsync($"UPDATE {tableName} SET UserName = 'NEW_NAME_2' WHERE Id = @Id", new { Id = 2 }, CancellationToken.None);
                    return await db.ToListAsync<Person>($"SELECT * FROM {tableName}", null, CancellationToken.None);
                }, CancellationToken.None).Unwrap();
            }, null, "Transaction Async");

        }

        public void ExecuteCachedDatabase(ICachedDatabase database, string databaseName, string tableName, int expectedItems = 0)
        {
            PerformOperation(() =>
            {
                var st = new ListResult(age: 35);
                return database.ToList<Person>("get_byAge", st);
            }, null, "ToListOp");
        }
    }
}
