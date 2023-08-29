using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thomas.Cache;
using Thomas.Database;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Legacy.Tests
{
    public class Tuple : ITestCase
    {
        public void Execute(IDatabase service, string databaseName, string tableName)
        {
            var stopWatch = new Stopwatch();

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var data = service.ToTuple<Person, Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}; SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", false);

                Console.ForegroundColor = ConsoleColor.Green;
                var builder = new StringBuilder();
                builder.AppendLine($"Method ToTuple<> ({databaseName})");
                builder.AppendLine($"Iteration {i + 1}, Rows processed List 1 : {data.Item1.Count()} - Rows processed List 2 : {data.Item2.Count()}");
                builder.AppendLine($"Elapse milliseconds: {stopWatch.ElapsedMilliseconds}");
                builder.AppendLine();

                Console.WriteLine(builder.ToString());
                stopWatch.Reset();
            }

            Console.WriteLine("");

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();

                var data = service.ToTupleOp<Person, Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}; SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", false);

                Console.ForegroundColor = ConsoleColor.Green;
                var builder = new StringBuilder();
                builder.AppendLine($"Method ToTupleOp<> ({databaseName})");
                builder.AppendLine($"Iteration {i + 1}, Rows processed List 1 : {data.Result.Item1.Count()} - Rows processed List 2 : {data.Result.Item2.Count()}");
                builder.AppendLine($"Elapse milliseconds: {stopWatch.ElapsedMilliseconds}");
                builder.AppendLine();

                Console.WriteLine(builder.ToString());
                stopWatch.Reset();
            }

            Console.WriteLine("");
        }

        public Task ExecuteAsync(IDatabase service, string databaseName, string tableName)
        {
            return Task.CompletedTask;
        }

        public void ExecuteCachedDatabase(ICachedDatabase database, string databaseName, string tableName)
        {
        }
    }
}
