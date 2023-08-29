using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Thomas.Cache;
using Thomas.Database;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Legacy.Tests
{
    public class Error : TestCase, ITestCase
    {
        public void Execute(IDatabase service, string databaseName, string tableName)
        {
            var stopWatch = new Stopwatch();

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var data = service.ToListOp<Person>($@"SELECT UserName2 FROM {tableName}", false);

                WriteTestResult(i + 1, "ToListOp<> error resilient", databaseName, stopWatch.ElapsedMilliseconds, $"by query, error: {data.ErrorMessage}");

                stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var data = service.ToSingleOp<Person>($@"SELECT UserName2 FROM {tableName}", false);

                WriteTestResult(i + 1, "ToSingleOp<> error resilient", databaseName, stopWatch.ElapsedMilliseconds, $"by query, error: {data.ErrorMessage}");

                stopWatch.Reset();
            }

            Console.WriteLine("");
        }

        public Task ExecuteAsync(IDatabase service, string databaseName, string tableName)
        {
            throw new NotImplementedException();
        }

        public void ExecuteCachedDatabase(ICachedDatabase database, string databaseName, string tableName)
        {
        }
    }
}
