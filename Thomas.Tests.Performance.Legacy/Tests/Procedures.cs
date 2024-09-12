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

        public void Execute(string db, string tableName, int expectedItems = 0)
        {
            PerformOperation(() =>
            {
                var st = new SearchTerm(id: 1);
                return DbHub.Use(db).TryExecute("get_byid", st);
            }, null, "TryExecute");

            PerformOperation(() =>
            {
                var st = new ListResult(age: 35);
                return DbHub.Use(db).TryFetchList<Person>("get_byAge", st);
            }, null, "TryFetchList");

            PerformOperation(() =>
            {
                return DbHub.Use(db).ExecuteTransaction((db) =>
                {
                    var data = db.FetchList<Person>($"SELECT * FROM {tableName}").ToArray();
                    db.Execute($"UPDATE {tableName} SET UserName = 'NEW_NAME' WHERE Id = @Id", new { data[0].Id });
                    db.Execute($"UPDATE {tableName} SET UserName = 'NEW_NAME_2' WHERE Id = @Id", new { data[1].Id });

                    return db.FetchList<Person>($"SELECT * FROM {tableName}");
                });

            }, null, "Transaction");

            PerformOperation(() =>
            {
                return DbHub.Use(db).ExecuteTransaction((db) =>
                {
                    db.Execute($"UPDATE {tableName} SET UserName = 'NEW_NAME_3' WHERE Id = @Id", new { Id = 1 });
                    db.Execute($"UPDATE {tableName} SET UserName = 'NEW_NAME_4' WHERE Id = @Id", new { Id = 2 });
                    return db.Rollback();
                });

            }, null, "Transaction Rollback");
        }

        public async Task ExecuteAsync(string db, string tableName, int expectedItems = 0)
        {
            await PerformOperationAsync(() =>
            {
                CancellationTokenSource source = new();
                source.CancelAfter(1);
                try
                {
                    DbHub.Use(db).ExecuteAsync("WAITFOR DELAY '00:00:03'", null, source.Token);
                }
                catch (System.Exception ex) { }

                return Task.FromResult(1);
            }, null, "ExecuteAsync Timeout", true);

            await PerformOperationAsync(() =>
            {
                CancellationTokenSource source = new();
                source.CancelAfter(1);
                return DbHub.Use(db).TryExecuteAsync("" +
                    "WAITFOR DELAY '00:00:03'", null, source.Token);
            }, null, "ExecuteOpAsync Timeout", true);

            await PerformOperationAsync(() =>
            {
                return DbHub.Use(db).ExecuteTransactionAsync(async (db, CancellationToken) =>
                {
                    await db.ExecuteAsync($"UPDATE {tableName} SET UserName = 'NEW_NAME' WHERE Id = @Id", new { Id = 1 });
                    await db.ExecuteAsync($"UPDATE {tableName} SET UserName = 'NEW_NAME_2' WHERE Id = @Id", new { Id = 2 });
                    return await db.FetchListAsync<Person>($"SELECT * FROM {tableName}", null);
                }, CancellationToken.None);
            }, null, "Transaction Async");


            await PerformOperationAsync(() =>
            {
                CancellationTokenSource source = new();
                source.CancelAfter(15);
                return DbHub.Use(db).ExecuteTransactionAsync(async (db, CancellationToken) =>
                {
                    return await db.ExecuteAsync($"WAITFOR DELAY '00:00:03'", null, CancellationToken);

                }, source.Token);
            }, null, "Transaction Async Timeout", shouldFail: true);

        }

        public void ExecuteCachedDatabase(string db, string tableName, int expectedItems = 0)
        {
            PerformOperation(() =>
            {
                var st = new ListResult(age: 35);
                return CachedDbHub.Use(db).FetchList<Person>("get_byAge", st);
            }, null, "FetchList");
        }
    }
}
