using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Thomas.Cache;
using Thomas.Database;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Legacy.Tests
{
    public class Procedures : TestCase, ITestCase
    {
        private Stopwatch _stopWatch;

        public Procedures()
        {
            _stopWatch = new Stopwatch();
        }

        public void Execute(IDatabase service, string databaseName, string tableName, int expectedItems = 0)
        {
            _stopWatch.Reset();

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var st = new SearchTerm(id: 1);

                service.ExecuteOp("get_byid", st);

                WriteTestResult(i + 1, "ExecuteOp", databaseName, _stopWatch.ElapsedMilliseconds, $"sp: get_byid, output userName: {st.UserName}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var st = new ListResult(age: 35);

                service.ToListOp<Person>("get_byAge", st);

                WriteTestResult(i + 1, "ToListOp", databaseName, _stopWatch.ElapsedMilliseconds, $"sp: get_byAge, output total: {st.Total}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var result = DbFactory.CreateDbContext("db1").ExecuteTransaction((db) =>
                {
                    var data = db.ToList<Person>($"SELECT * FROM {tableName}").ToArray();
                    db.Execute($"UPDATE {tableName} SET UserName = 'NEW_NAME' WHERE Id = @Id", new { data[0].Id });
                    db.Execute($"UPDATE {tableName} SET UserName = 'NEW_NAME_2' WHERE Id = @Id", new { data[1].Id });

                    return db.ToList<Person>($"SELECT * FROM {tableName}");
                });

                WriteTestResult(i + 1, "Transaction", databaseName, _stopWatch.ElapsedMilliseconds, $"transaction 1, output total: {result.Count()}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var transactionResult = DbFactory.CreateDbContext("db1").ExecuteTransaction((db) =>
                {
                    db.Execute($"UPDATE {tableName} SET UserName = 'NEW_NAME_3' WHERE Id = @Id", new { Id = 1 });
                    db.Execute($"UPDATE {tableName} SET UserName = 'NEW_NAME_4' WHERE Id = @Id", new { Id = 2 });
                    return db.Rollback();
                });

                WriteTestResult(i + 1, "Transaction Rollback", databaseName, _stopWatch.ElapsedMilliseconds, $"transaction 2, commited: {transactionResult}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");
        }

        public async Task ExecuteAsync(IDatabase service, string databaseName, string tableName, int expectedItems = 0)
        {
            _stopWatch.Reset();

            for (int i = 0; i < 10; i++)
            {
                CancellationTokenSource source = new CancellationTokenSource();
                source.CancelAfter(1);

                _stopWatch.Start();

                try
                {
                    await service.ExecuteAsync("WAITFOR DELAY '00:00:10'", false, source.Token);
                }
                catch
                {
                    WriteTestResult(i + 1, "ExecuteAsync", databaseName, _stopWatch.ElapsedMilliseconds, $"script WAITFOR DELAY, test cancellation token error");
                }

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                CancellationTokenSource source = new CancellationTokenSource();
                source.CancelAfter(100);

                _stopWatch.Start();

                var data = await service.ExecuteOpAsync("WAITFOR DELAY '00:00:10'", false, source.Token);

                WriteTestResult(i + 1, "ExecuteOpAsync", databaseName, _stopWatch.ElapsedMilliseconds, $"by query, cancelled = {data.Cancelled}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var result = await DbFactory.CreateDbContext("db2").ExecuteTransactionAsync(async (db) =>
                {
                    await db.ExecuteAsync($"UPDATE {tableName} SET UserName = 'NEW_NAME' WHERE Id = @Id", new { Id = 1 }, CancellationToken.None);
                    await db.ExecuteAsync($"UPDATE {tableName} SET UserName = 'NEW_NAME_2' WHERE Id = @Id", new { Id = 2 }, CancellationToken.None);
                    return await db.ToListAsync<Person>($"SELECT * FROM {tableName}", null, CancellationToken.None);
                }, CancellationToken.None).Unwrap();

                WriteTestResult(i + 1, "Transaction Async", databaseName, _stopWatch.ElapsedMilliseconds, $"transaction async, output total: {result.Count()}");

                _stopWatch.Reset();
            }
        }

        public void ExecuteCachedDatabase(ICachedDatabase database, string databaseName, string tableName, int expectedItems = 0)
        {
            _stopWatch.Reset();

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var st = new ListResult(age: 35);

                database.ToList<Person>("get_byAge", st);

                WriteTestResult(i + 1, "ToListOp", databaseName, _stopWatch.ElapsedMilliseconds, $"sp: get_byAge, output total: {st.Total}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");
        }
    }
}
