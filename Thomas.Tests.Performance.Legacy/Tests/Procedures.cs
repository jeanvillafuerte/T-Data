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
                return DbFactory.GetDbContext(db).ExecuteOp("get_byid", st);
            }, null, "ExecuteOp");

            PerformOperation(() =>
            {
                var st = new ListResult(age: 35);
                return DbFactory.GetDbContext(db).ToListOp<Person>("get_byAge", st);
            }, null, "ToListOp");

            PerformOperation(() =>
            {
                return DbFactory.GetDbContext(db).ExecuteTransaction((db) =>
                {
                    var data = db.ToList<Person>($"SELECT * FROM {tableName}").ToArray();
                    db.Execute($"UPDATE {tableName} SET UserName = 'NEW_NAME' WHERE Id = @Id", new { data[0].Id });
                    db.Execute($"UPDATE {tableName} SET UserName = 'NEW_NAME_2' WHERE Id = @Id", new { data[1].Id });

                    return db.ToList<Person>($"SELECT * FROM {tableName}");
                });

            }, null, "Transaction");

            PerformOperation(() =>
            {
                return DbFactory.GetDbContext(db).ExecuteTransaction((db) =>
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
                    DbFactory.GetDbContext(db).ExecuteAsync("WAITFOR DELAY '00:00:03'", null, source.Token);
                }
                catch (System.Exception ex) { }

                return Task.FromResult(1);
            }, null, "ExecuteAsync Timeout", true);

            await PerformOperationAsync(() =>
            {
                CancellationTokenSource source = new();
                source.CancelAfter(1);
                return DbFactory.GetDbContext(db).ExecuteOpAsync("" +
                    "WAITFOR DELAY '00:00:03'", null, source.Token);
            }, null, "ExecuteOpAsync Timeout", true);

            await PerformOperationAsync(() =>
            {
                return DbFactory.GetDbContext(db).ExecuteTransactionAsync(async (db, CancellationToken) =>
                {
                    await db.ExecuteAsync($"UPDATE {tableName} SET UserName = 'NEW_NAME' WHERE Id = @Id", new { Id = 1 });
                    await db.ExecuteAsync($"UPDATE {tableName} SET UserName = 'NEW_NAME_2' WHERE Id = @Id", new { Id = 2 });
                    return await db.ToListAsync<Person>($"SELECT * FROM {tableName}", null);
                }, CancellationToken.None);
            }, null, "Transaction Async");


            await PerformOperationAsync(() =>
            {
                CancellationTokenSource source = new();
                source.CancelAfter(15);
                return DbFactory.GetDbContext(db).ExecuteTransactionAsync(async (db, CancellationToken) =>
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
                return CachedDbFactory.GetDbContext(db).ToList<Person>("get_byAge", st);
            }, null, "ToListOp");
        }
    }
}
