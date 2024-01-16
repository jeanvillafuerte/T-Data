using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Thomas.Cache;
using Thomas.Database;

namespace Thomas.Tests.Performance.Legacy.Tests
{
    public interface ITestCase
    {
        void Execute(IDatabase service, string databaseName, string tableName, int expectedItems = 0);
        Task ExecuteAsync(IDatabase service, string databaseName, string tableName, int expectedItems = 0);
        void ExecuteCachedDatabase(ICachedDatabase database, string databaseName, string tableName, int expectedItems = 0);
    }

    public abstract class TestCase
    {
        public void WriteTestResult(int iteration, string method, string database, long ElapsedMilliseconds, string additional, int rows = -1, int expectedRows = -1)
        {
            string expectedResult;
            if (expectedRows > -1)
                expectedResult = $" (expected: {expectedRows}) | RESULT: {(rows == expectedRows ? "OK" : "ERROR")}";
            else
                expectedResult = " | RESULT: OK";

            Console.WriteLine($"{method} ({database}) Iteration {iteration.ToString().PadLeft(3, '0')} Elapse ml: {ElapsedMilliseconds.ToString().PadLeft(8, '0')} -> {additional}{expectedResult}");
        }

        public async Task<int> CountAsyncEnumerable<T>(IAsyncEnumerable<T> data)
        {
            int counter = 0;
            await foreach (var item in data)
            {
                counter++;
            }

            return counter;
        }
    }
}
