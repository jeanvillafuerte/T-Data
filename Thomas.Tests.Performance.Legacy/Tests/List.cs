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
        private Stopwatch _stopWatch;

        public List()
        {
            _stopWatch = new Stopwatch();
        }

        public void Execute(IDatabase service, string databaseName, string tableName, int expectedItems = 0)
        {
            _stopWatch.Reset();

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var data = service.ToList<Person>($"SELECT * FROM {tableName} WHERE Id > @Id", new { Id = 0 });

                WriteTestResult(i + 1, "ToList<>", databaseName, _stopWatch.ElapsedMilliseconds, $"by query, records: {data.Count()}", data.Count(), expectedItems);

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var data = service.ToListOp<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}");

                WriteTestResult(i + 1, "ToListOp<>", databaseName, _stopWatch.ElapsedMilliseconds, $"by query, records: {data.Result.Count()}", data.Result.Count(), expectedItems);

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var data = service.ToList<Person>($@"get_{tableName}", new { age = 5 });

                WriteTestResult(i + 1, "ToList<>", databaseName, _stopWatch.ElapsedMilliseconds, $"sp: get_{tableName}, records: {data.Count()}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var data = service.ToListOp<Person>($@"get_{tableName}", new { age = 5 });

                WriteTestResult(i + 1, "ToListOp<>", databaseName, _stopWatch.ElapsedMilliseconds, $"sp: get_{tableName}, records: {data.Result.Count()}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var st = new ListResult(age: 35);

                var data = service.ToList<Person>("get_byAge", st);

                WriteTestResult(i + 1, "ToList<>", databaseName, _stopWatch.ElapsedMilliseconds, $"sp: get_byAge, ouput Total: {st.Total}, records: {data.Count()}");

                _stopWatch.Reset();
            }

        }

        public async Task ExecuteAsync(IDatabase service, string databaseName, string tableName, int expectedItems = 0)
        {
            _stopWatch.Reset();

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var data = await service.ToListAsync<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", null, CancellationToken.None);

                WriteTestResult(i + 1, "ToListAsync<>", databaseName, _stopWatch.ElapsedMilliseconds, $"by query, records: {data.Count()}", data.Count(), expectedItems);

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var data = await service.ToListOpAsync<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", null, CancellationToken.None);

                WriteTestResult(i + 1, "ToListOpAsync<>", databaseName, _stopWatch.ElapsedMilliseconds, $"by query, records: {data.Result.Count()}", data.Result.Count(), expectedItems);

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var data = await service.ToListAsync<Person>($@"get_{tableName}", new { age = 5 }, CancellationToken.None);

                WriteTestResult(i + 1, "ToListAsync<>", databaseName, _stopWatch.ElapsedMilliseconds, $"sp: get_{tableName}, records: {data.Count()}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var data = await service.ToListOpAsync<Person>($@"get_{tableName}", new { age = 5 }, CancellationToken.None);

                WriteTestResult(i + 1, "ToListOpAsync<>", databaseName, _stopWatch.ElapsedMilliseconds, $"sp: get_{tableName}, records: {data.Result.Count()}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var st = new ListResult(age: 35);

                var data = await service.ToListAsync<Person>("get_byAge", st, CancellationToken.None);

                WriteTestResult(i + 1, "ToListAsync<>", databaseName, _stopWatch.ElapsedMilliseconds, $"sp: get_byAge, ouput Total: {st.Total}, records: {data.Count()}");

                _stopWatch.Reset();
            }

            for (int i = 0; i < 10; i++)
            {

                CancellationTokenSource source = new CancellationTokenSource();
                source.CancelAfter(100);

                _stopWatch.Start();

                var data = await service.ToListOpAsync<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", null, source.Token);

                WriteTestResult(i + 1, "ToListOpAsync<> Cancelled", databaseName, _stopWatch.ElapsedMilliseconds, $"by query, cancelled = {data.Cancelled}");

                _stopWatch.Reset();
            }
        }

        public void ExecuteCachedDatabase(ICachedDatabase database, string databaseName, string tableName, int expectedItems = 0)
        {
            _stopWatch.Reset();

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var data = database.ToList<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}");

                WriteTestResult(i + 1, "ToList<>", databaseName, _stopWatch.ElapsedMilliseconds, $"by query, records: {data.Count()}", data.Count(), expectedItems);

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var data = database.ToList<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", null, refresh: true);

                WriteTestResult(i + 1, "ToList<> (refresh)", databaseName, _stopWatch.ElapsedMilliseconds, $"by query, records: {data.Count()}", data.Count(), expectedItems);

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var data = database.ToList<Person>($@"get_{tableName}", new { age = 5 });

                WriteTestResult(i + 1, "ToList<>", databaseName, _stopWatch.ElapsedMilliseconds, $"sp: get_{tableName}, records: {data.Count()}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var st = new ListResult(age: 35);

                var data = database.ToList<Person>("get_byAge", st);

                WriteTestResult(i + 1, "ToList<>", databaseName, _stopWatch.ElapsedMilliseconds, $"sp: get_byAge, ouput Total: {st.Total}, records: {data.Count()}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var st = new ListResult(age: 35);

                var data = database.ToList<Person>("get_byAge", st, refresh: true);

                WriteTestResult(i + 1, "ToList<> (refresh)", databaseName, _stopWatch.ElapsedMilliseconds, $"sp: get_byAge, ouput Total: {st.Total}, records: {data.Count()}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");
        }
    }
}
