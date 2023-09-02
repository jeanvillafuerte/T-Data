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
    public class List : TestCase, ITestCase
    {
        public void Execute(IDatabase service, string databaseName, string tableName)
        {
            var stopWatch = new Stopwatch();

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var data = service.ToList<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", false);

                WriteTestResult(i + 1, "ToList<>", databaseName, stopWatch.ElapsedMilliseconds, $"by query, records: {data.Count()}");

                stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var data = service.ToListOp<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", false);

                WriteTestResult(i + 1, "ToListOp<>", databaseName, stopWatch.ElapsedMilliseconds, $"by query, records: {data.Result.Count()}");

                stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var data = service.ToList<Person>(new { age = 5 }, $@"get_{tableName}", true);

                WriteTestResult(i + 1, "ToList<>", databaseName, stopWatch.ElapsedMilliseconds, $"sp: get_{tableName}, records: {data.Count()}");

                stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var data = service.ToListOp<Person>(new { age = 5 }, $@"get_{tableName}");

                WriteTestResult(i + 1, "ToListOp<>", databaseName, stopWatch.ElapsedMilliseconds, $"sp: get_{tableName}, records: {data.Result.Count()}");

                stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var st = new ListResult(age: 35);

                var data = service.ToList<Person>(st, "get_byAge", true);

                WriteTestResult(i + 1, "ToList<>", databaseName, stopWatch.ElapsedMilliseconds, $"sp: get_byAge, ouput Total: {st.Total}, records: {data.Count()}");

                stopWatch.Reset();
            }

        }

        public async Task ExecuteAsync(IDatabase service, string databaseName, string tableName)
        {
            var stopWatch = new Stopwatch();

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var data = await service.ToListAsync<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", false, CancellationToken.None);

                WriteTestResult(i + 1, "ToListAsync<>", databaseName, stopWatch.ElapsedMilliseconds, $"by query, records: {data.Count()}");

                stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var data = await service.ToListOpAsync<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", false, CancellationToken.None);

                WriteTestResult(i + 1, "ToListOpAsync<>", databaseName, stopWatch.ElapsedMilliseconds, $"by query, records: {data.Result.Count()}");

                stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var data = await service.ToListAsync<Person>(new { age = 5 }, $@"get_{tableName}", CancellationToken.None);

                WriteTestResult(i + 1, "ToListAsync<>", databaseName, stopWatch.ElapsedMilliseconds, $"sp: get_{tableName}, records: {data.Count()}");

                stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var data = await service.ToListOpAsync<Person>(new { age = 5 }, $@"get_{tableName}", CancellationToken.None);

                WriteTestResult(i + 1, "ToListOpAsync<>", databaseName, stopWatch.ElapsedMilliseconds, $"sp: get_{tableName}, records: {data.Result.Count()}");

                stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var st = new ListResult(age: 35);

                var data = await service.ToListAsync<Person>(st, "get_byAge", CancellationToken.None);

                WriteTestResult(i + 1, "ToListAsync<>", databaseName, stopWatch.ElapsedMilliseconds, $"sp: get_byAge, ouput Total: {st.Total}, records: {data.Count()}");

                stopWatch.Reset();
            }

            for (int i = 0; i < 10; i++)
            {

                CancellationTokenSource source = new CancellationTokenSource();
                source.CancelAfter(100);

                stopWatch.Start();

                var data = await service.ToListOpAsync<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", false, source.Token);

                WriteTestResult(i + 1, "ToListOpAsync<> Cancelled", databaseName, stopWatch.ElapsedMilliseconds, $"by query, cancelled = {data.Cancelled}");

                stopWatch.Reset();
            }
        }

        public void ExecuteCachedDatabase(ICachedDatabase database, string databaseName, string tableName)
        {
            var stopWatch = new Stopwatch();

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var data = database.ToList<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", false);

                WriteTestResult(i + 1, "ToList<>", databaseName, stopWatch.ElapsedMilliseconds, $"by query, records: {data.Count()}");

                stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var data = database.ToList<Person>(new { age = 5 }, $@"get_{tableName}");

                WriteTestResult(i + 1, "ToList<>", databaseName, stopWatch.ElapsedMilliseconds, $"sp: get_{tableName}, records: {data.Count()}");

                stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var st = new ListResult(age: 35);

                var data = database.ToList<Person>(st, "get_byAge");

                WriteTestResult(i + 1, "ToList<>", databaseName, stopWatch.ElapsedMilliseconds, $"sp: get_byAge, ouput Total: {st.Total}, records: {data.Count()}");

                stopWatch.Reset();
            }

            Console.WriteLine("");
        }
    }
}
