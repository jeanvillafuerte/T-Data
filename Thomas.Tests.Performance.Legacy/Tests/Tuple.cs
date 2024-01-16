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
    public class Tuple : TestCase, ITestCase
    {
        private Stopwatch _stopWatch;

        public Tuple()
        {
            _stopWatch = new Stopwatch();
        }

        public void Execute(IDatabase service, string databaseName, string tableName, int expectedItems = 0)
        {
            _stopWatch.Reset();

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var data = service.ToTuple<Person, Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}; SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}");

                WriteTestResult(i + 1, "ToTuple<>", databaseName, _stopWatch.ElapsedMilliseconds, $"by query, records list 1: {data.Item1.Count()}, records list 2: {data.Item2.Count()}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var data = service.ToTupleOp<Person, Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}; SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}");

                WriteTestResult(i + 1, "ToTupleOp<>", databaseName, _stopWatch.ElapsedMilliseconds, $"by query, records list 1: {data.Result.Item1.Count()}, records list 2: {data.Result.Item2.Count()}");

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

                var data = await service.ToTupleAsync<Person, Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}; SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", null, CancellationToken.None);

                WriteTestResult(i + 1, "ToTuple<>", databaseName, _stopWatch.ElapsedMilliseconds, $"by query, records list 1: {data.Item1.Count()}, records list 2: {data.Item2.Count()}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var data = await service.ToTupleOpAsync<Person, Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}; SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", null, CancellationToken.None);

                WriteTestResult(i + 1, "ToTupleOp<>", databaseName, _stopWatch.ElapsedMilliseconds, $"by query, records list 1: {data.Result.Item1.Count()}, records list 2: {data.Result.Item2.Count()}");

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

                var data = database.ToTuple<Person, Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}; SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}");

                WriteTestResult(i + 1, "ToTuple<T1,T2>", databaseName, _stopWatch.ElapsedMilliseconds, $"by query, records list 1: {data.Item1.Count()}, records list 2: {data.Item2.Count()}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                _stopWatch.Start();

                var data = database.ToTuple<Person, Person, Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}; SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}; SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}");

                WriteTestResult(i + 1, "ToTuple<T1,T2,T3>", databaseName, _stopWatch.ElapsedMilliseconds, $"by query, records list 1: {data.Item1.Count()}, records list 2: {data.Item2.Count()}, records list 3: {data.Item3.Count()}");

                _stopWatch.Reset();
            }

            Console.WriteLine("");

        }
    }
}
