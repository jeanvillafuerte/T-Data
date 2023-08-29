using System;
using System.Threading.Tasks;
using Thomas.Cache;
using Thomas.Database;

namespace Thomas.Tests.Performance.Legacy.Tests
{
    public interface ITestCase
    {
        void Execute(IDatabase service, string databaseName, string tableName);
        Task ExecuteAsync(IDatabase service, string databaseName, string tableName);
        void ExecuteCachedDatabase(ICachedDatabase database, string databaseName, string tableName);
    }

    public abstract class TestCase
    {
        public void WriteTestResult(int iteration, string method, string database, long ElapsedMilliseconds, string additional)
        {
            Console.WriteLine($"{method} ({database}) Iteration {iteration.ToString().PadLeft(3, '0')} Elapse ml: {ElapsedMilliseconds.ToString().PadLeft(10, '0')} -> {additional}");
        }
    }
}
