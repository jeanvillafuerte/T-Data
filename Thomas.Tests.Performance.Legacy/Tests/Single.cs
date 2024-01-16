using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Thomas.Cache;
using Thomas.Database;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Legacy.Tests
{
    public class Single : TestCase, ITestCase
    {
        private Stopwatch _stopWatch;

        public Single()
        {
            _stopWatch = new Stopwatch();
        }

        public void Execute(IDatabase service, string databaseName, string tableName, int expectedItems = 0)
        {
            _stopWatch.Reset();

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var data = service.ToSingle<Person>($@"SELECT TOP 1 UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}");

                WriteTestResult(i + 1, "ToSingle<>", databaseName, _stopWatch.ElapsedMilliseconds, $"by query, user name: {data.UserName}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var data = service.ToSingleOp<Person>($@"SELECT TOP 1 UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}");

                WriteTestResult(i + 1, "ToSingleOp<>", databaseName, _stopWatch.ElapsedMilliseconds, $"by query, user name: {data.Result.UserName}");

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

                var data = await service.ToSingleAsync<Person>($@"SELECT TOP 1 UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", null, CancellationToken.None);

                WriteTestResult(i + 1, "ToSingle<>", databaseName, _stopWatch.ElapsedMilliseconds, $"by query, user name: {data.UserName}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var data = await service.ToSingleOpAsync<Person>($@"SELECT TOP 1 UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", null, CancellationToken.None);

                WriteTestResult(i + 1, "ToSingleOp<>", databaseName, _stopWatch.ElapsedMilliseconds, $"by query, user name: {data.Result.UserName}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");
        }

        public void ExecuteCachedDatabase(ICachedDatabase database, string databaseName, string tableName, int expectedItems = 0)
        {
            _stopWatch.Reset();

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var data = database.ToSingle<Person>($@"SELECT TOP 1 UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}");

                WriteTestResult(i + 1, "ToSingle<>", databaseName, _stopWatch.ElapsedMilliseconds, $"by query, user name: {data.UserName}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");
        }
    }
}
