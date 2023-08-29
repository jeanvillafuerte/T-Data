using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Thomas.Cache;
using Thomas.Database;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Legacy.Tests
{
    public class Procedures : TestCase, ITestCase
    {
        public void Execute(IDatabase service, string databaseName, string tableName)
        {
            var stopWatch = new Stopwatch();

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var st = new SearchTerm(id: 1);

                service.ExecuteOp(st, "get_byid");

                WriteTestResult(i + 1, "ExecuteOp", databaseName, stopWatch.ElapsedMilliseconds, $"sp: get_byid, output userName: {st.UserName}");

                stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var st = new ListResult(age: 35);

                service.ToListOp<Person>(st, "get_byAge");

                WriteTestResult(i + 1, "ToListOp", databaseName, stopWatch.ElapsedMilliseconds, $"sp: get_byAge, output total: {st.Total}");

                stopWatch.Reset();
            }

            Console.WriteLine("");
        }

        public async Task ExecuteAsync(IDatabase service, string databaseName, string tableName)
        {
            var stopWatch = new Stopwatch();

            for (int i = 0; i < 10; i++)
            {
                CancellationTokenSource source = new CancellationTokenSource();
                source.CancelAfter(1);

                stopWatch.Start();

                var st = new SearchTerm(id: 1);

                try
                {
                    await service.ExecuteAsync("WAITFOR DELAY '00:00:10'", false, source.Token);
                }
                catch
                {
                    WriteTestResult(i + 1, "ExecuteAsync", databaseName, stopWatch.ElapsedMilliseconds, $"script WAITFOR DELAY, test cancellation token error");
                }

                stopWatch.Reset();
            }

            for (int i = 0; i < 10; i++)
            {
                CancellationTokenSource source = new CancellationTokenSource();
                source.CancelAfter(100);

                stopWatch.Start();

                var data = await service.ExecuteOpAsync("WAITFOR DELAY '00:00:10'", false, source.Token);

                WriteTestResult(i + 1, "ExecuteOpAsync", databaseName, stopWatch.ElapsedMilliseconds, $"by query, cancelled = {data.Cancelled}");

                stopWatch.Reset();
            }

            Console.WriteLine("");
        }

        public void ExecuteCachedDatabase(ICachedDatabase database, string databaseName, string tableName)
        {
        }
    }
}
