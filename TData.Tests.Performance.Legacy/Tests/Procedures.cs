using System.Threading;
using System.Threading.Tasks;
using TData.Cache;
using TData.Tests.Performance.Entities;

namespace TData.Tests.Performance.Legacy.Tests
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
                return DbHub.Use(in db).TryExecute("get_byid", st);
            }, "TryExecute");

            PerformOperation(() =>
            {
                var st = new ListResult(age: 35);
                return DbHub.Use(in db).TryFetchList<Person>("get_byAge", st);
            }, null, "TryFetchList");

            PerformOperation(() =>
            {
                return DbHub.Use(in db).ExecuteTransaction((db) =>
                {
                    var data = db.FetchList<Person>($"SELECT * FROM {tableName}").ToArray();
                    db.Execute($"UPDATE {tableName} SET UserName = 'NEW_NAME' WHERE Id = @Id", new { data[0].Id });
                    db.Execute($"UPDATE {tableName} SET UserName = 'NEW_NAME_2' WHERE Id = @Id", new { data[1].Id });

                    return db.FetchList<Person>($"SELECT * FROM {tableName}");
                });

            }, null, "Transaction");

            PerformOperation(() =>
            {
                return DbHub.Use(in db).ExecuteTransaction((db) =>
                {
                    db.Execute($"UPDATE {tableName} SET UserName = 'NEW_NAME_3' WHERE Id = @Id", new { Id = 1 });
                    db.Execute($"UPDATE {tableName} SET UserName = 'NEW_NAME_4' WHERE Id = @Id", new { Id = 2 });
                    return db.Rollback();
                });

            }, "Transaction Rollback");
        }

        public void ExecuteAsync(string db, string tableName, int expectedItems = 0)
        {
            PerformOperationAsync(() =>
            {
                CancellationTokenSource source = new();
                source.CancelAfter(1);
                try
                {
                    DbHub.Use(in db).ExecuteAsync("WAITFOR DELAY '00:00:03'", null, source.Token);
                }
                catch (System.Exception) { }

                return Task.FromResult(1);
            }, "ExecuteAsync Timeout", true);

            PerformOperationAsync(() =>
            {
                CancellationTokenSource source = new();
                source.CancelAfter(1);
                return DbHub.Use(in db).TryExecuteAsync("" +
                    "WAITFOR DELAY '00:00:03'", null, source.Token);
            }, "ExecuteOpAsync Timeout", true);

            PerformOperationAsync(() =>
            {
                return DbHub.Use(in db).ExecuteTransactionAsync(async (db, CancellationToken) =>
                {
                    await db.ExecuteAsync($"UPDATE {tableName} SET UserName = 'NEW_NAME' WHERE Id = @Id", new { Id = 1 });
                    await db.ExecuteAsync($"UPDATE {tableName} SET UserName = 'NEW_NAME_2' WHERE Id = @Id", new { Id = 2 });
                    return await db.FetchListAsync<Person>($"SELECT * FROM {tableName}", null);
                }, CancellationToken.None);
            }, "Transaction Async");


            PerformOperationAsync(() =>
            {
                CancellationTokenSource source = new();
                source.CancelAfter(15);
                return DbHub.Use(in db).ExecuteTransactionAsync(async (db, CancellationToken) =>
                {
                    return await db.ExecuteAsync($"WAITFOR DELAY '00:00:03'", null, CancellationToken);

                }, source.Token);
            }, "Transaction Async Timeout", shouldFail: true);

        }

        public void ExecuteCachedDatabase(string db, string tableName, int expectedItems = 0)
        {
            PerformOperation(() =>
            {
                var st = new ListResult(age: 35);
                return CachedDbHub.Use(in db).FetchList<Person>("get_byAge", st);
            }, null, "FetchList");
        }
    }
}
