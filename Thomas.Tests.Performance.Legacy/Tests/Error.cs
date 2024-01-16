using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Thomas.Cache;
using Thomas.Database;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Legacy.Tests
{
    public class Error : TestCase, ITestCase
    {
        private Stopwatch _stopWatch;

        public Error()
        {
            _stopWatch = new Stopwatch();
        }

        public void Execute(IDatabase service, string databaseName, string tableName, int expectedItems = 0)
        {
            _stopWatch.Reset();

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var data = service.ToListOp<Person>($@"SELECT UserName2 FROM {tableName}", false);

                WriteTestResult(i + 1, "ToListOp<> error resilient", databaseName, _stopWatch.ElapsedMilliseconds, $"by query, error: {data.ErrorMessage}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var data = service.ToSingleOp<Person>($@"SELECT UserName2 FROM {tableName}", false);

                WriteTestResult(i + 1, "ToSingleOp<> error resilient", databaseName, _stopWatch.ElapsedMilliseconds, $"by query, error: {data.ErrorMessage}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");
        }

        public async Task ExecuteAsync(IDatabase service, string databaseName, string tableName, int expectedItems = 0)
        {
            _stopWatch.Reset();

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var data = await service.ToListOpAsync<Person>($@"SELECT UserName2 FROM {tableName}", null, CancellationToken.None);

                WriteTestResult(i + 1, "ToListOp<> error resilient", databaseName, _stopWatch.ElapsedMilliseconds, $"by query, error: {data.ErrorMessage}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var data = await service.ToSingleOpAsync<Person>($@"SELECT UserName2 FROM {tableName}", null, CancellationToken.None);

                WriteTestResult(i + 1, "ToSingleOp<> error resilient", databaseName, _stopWatch.ElapsedMilliseconds, $"by query, error: {data.ErrorMessage}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");
        }

        public void ExecuteCachedDatabase(ICachedDatabase database, string databaseName, string tableName, int expectedItems = 0)
        {
            //not supported
        }
    }
}
