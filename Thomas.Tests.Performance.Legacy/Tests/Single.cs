using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Thomas.Cache;
using Thomas.Database;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Legacy.Tests
{
    public class Single : TestCase, ITestCase
    {
        public void Execute(IDatabase service, string databaseName, string tableName)
        {
            var stopWatch = new Stopwatch();

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var data = service.ToSingle<Person>($@"SELECT TOP 1 UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", false);

                WriteTestResult(i + 1, "ToSingle<>", databaseName, stopWatch.ElapsedMilliseconds, $"by query, user name: {data.UserName}");

                stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var data = service.ToSingleOp<Person>($@"SELECT TOP 1 UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", false);

                WriteTestResult(i + 1, "ToSingleOp<>", databaseName, stopWatch.ElapsedMilliseconds, $"by query, user name: {data.Result.UserName}");

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
            var stopWatch = new Stopwatch();

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var data = database.ToSingle<Person>($@"SELECT TOP 1 UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", false);

                WriteTestResult(i + 1, "ToSingle<>", databaseName, stopWatch.ElapsedMilliseconds, $"by query, user name: {data.UserName}");

                stopWatch.Reset();
            }

            Console.WriteLine("");
        }
    }
}
