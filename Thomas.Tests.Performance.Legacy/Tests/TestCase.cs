using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thomas.Database;

namespace Thomas.Tests.Performance.Legacy.Tests
{
    public abstract class TestCase(string databaseName)
    {
        protected readonly string _databaseName = databaseName;

        public void PerformOperation(Action Operation, int? expectedItems, string operationName = "")
        {

            var stopWatch = new Stopwatch();

            var builder = new StringBuilder();
            builder.Append($"\tOperation: {operationName}");
            builder.AppendLine("");

            for (int i = 0; i < 1000; i++)
            {
                stopWatch.Start();

                Operation.Invoke();
                var elapsed = stopWatch.ElapsedMilliseconds;

                builder.AppendLine("\t" + WriteTestResult(i + 1, _databaseName, elapsed, default, default, expectedItems ?? -1));
                stopWatch.Reset();
            }

            builder.AppendLine("");
            Console.WriteLine(builder.ToString());
        }

        public void PerformOperation<T>(Func<T> Operation, int? expectedItems, string operationName = "")
        {

            var stopWatch = new Stopwatch();

            var builder = new StringBuilder();
            builder.Append($"\tOperation: {operationName}");
            builder.AppendLine("");

            for (int i = 0; i < 1000; i++)
            {
                stopWatch.Start();

                try
                {
                    Operation.Invoke();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                var elapsed = stopWatch.ElapsedMilliseconds;

                builder.AppendLine("\t" + WriteTestResult(i + 1, _databaseName, elapsed, default, default, expectedItems ?? -1));
                stopWatch.Reset();
            }

            builder.AppendLine("");
            Console.WriteLine(builder.ToString());
        }

        public void PerformOperation<T>(Func<DbOpResult<List<T>>> Operation, int? expectedItems, string operationName = "")
        {

            var stopWatch = new Stopwatch();

            var builder = new StringBuilder();
            builder.Append($"\tOperation: {operationName}");
            builder.AppendLine("");

            for (int i = 0; i < 1000; i++)
            {
                stopWatch.Start();

                var result = Operation.Invoke();

                var elapsed = stopWatch.ElapsedMilliseconds;

                builder.AppendLine("\t" + WriteTestResult(i + 1, _databaseName, elapsed, default, result.Result?.Count ?? 0, expectedItems ?? -1, !result.Success));
                stopWatch.Reset();
            }

            builder.AppendLine("");
            Console.WriteLine(builder.ToString());
        }

        public void PerformOperation<T>(Func<List<T>> Operation, int? expectedItems, string operationName = "")
        {

            var stopWatch = new Stopwatch();

            var builder = new StringBuilder();
            builder.Append($"\tOperation: {operationName}");
            builder.AppendLine("");

            for (int i = 0; i < 1000; i++)
            {
                stopWatch.Start();
                int count = 0;
                try
                {
                    count = Operation.Invoke().Count;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                var elapsed = stopWatch.ElapsedMilliseconds;

                builder.AppendLine("\t" + WriteTestResult(i + 1, _databaseName, elapsed, default, count, expectedItems ?? -1));
                stopWatch.Reset();
            }

            builder.AppendLine("");
            Console.WriteLine(builder.ToString());
        }

        public async Task PerformOperationAsync<T>(Func<Task<T>> Operation, int? expectedItems, string operationName = "", bool shouldFail = false)
        {

            var stopWatch = new Stopwatch();

            var builder = new StringBuilder();
            builder.Append($"\tOperation: {operationName}");
            builder.AppendLine("");

            for (int i = 0; i < 1000; i++)
            {
                stopWatch.Start();

                if (shouldFail)
                {
                    try
                    {
                        await Operation.Invoke();
                    }
                    catch (Exception) { }
                }
                else
                {
                    await Operation.Invoke();
                }

                var elapsed = stopWatch.ElapsedMilliseconds;

                builder.AppendLine("\t" + WriteTestResult(i + 1, _databaseName, elapsed, default, default, expectedItems ?? -1));
                stopWatch.Reset();
            }

            builder.AppendLine("");
            Console.WriteLine(builder.ToString());
        }

        public async Task PerformOperationAsync<T>(Func<Task<List<T>>> operation, int? expectedItems, string operationName = "", bool shouldFail = false)
        {

            var stopWatch = new Stopwatch();

            var builder = new StringBuilder();
            builder.Append($"\tOperation: {operationName}");
            builder.AppendLine("");
            for (int i = 0; i < 1000; i++)
            {
                stopWatch.Start();
                int count = 0;

                if (shouldFail)
                {
                    try
                    {
                        count = (await operation.Invoke()).Count;
                    }
                    catch (Exception) { }
                }
                else
                {
                    count = (await operation.Invoke()).Count;
                }

                var elapsed = stopWatch.ElapsedMilliseconds;

                builder.AppendLine("\t" + WriteTestResult(i + 1, _databaseName, elapsed, default, count, expectedItems ?? -1));
                stopWatch.Reset();
            }

            builder.AppendLine("");
            Console.WriteLine(builder.ToString());
        }

        public void PerformOperation<T>(Func<DbOpResult<IEnumerable<T>>> Operation, int? expectedItems, string operationName = "")
        {

            var stopWatch = new Stopwatch();

            var builder = new StringBuilder();
            builder.Append($"\tOperation: {operationName}");
            builder.AppendLine("");

            for (int i = 0; i < 1000; i++)
            {
                stopWatch.Start();

                var data = Operation.Invoke();
                var elapsed = stopWatch.ElapsedMilliseconds;
                var count = data.Result?.Count() ?? -1;

                builder.AppendLine("\t" + WriteTestResult(i + 1, _databaseName, elapsed, default, count, expectedItems ?? -1));
                stopWatch.Reset();
            }

            builder.AppendLine("");
            Console.WriteLine(builder.ToString());
        }

        public void PerformOperation<T>(Func<IEnumerable<T>> Operation, int? expectedItems, string operationName = "")
        {

            var stopWatch = new Stopwatch();

            var builder = new StringBuilder();
            builder.Append($"\tOperation: {operationName}");
            builder.AppendLine("");

            for (int i = 0; i < 1000; i++)
            {
                stopWatch.Start();

                var data = Operation.Invoke();
                var elapsed = stopWatch.ElapsedMilliseconds;
                builder.AppendLine("\t" + WriteTestResult(i + 1, _databaseName, elapsed, default, data.Count(), expectedItems ?? -1));

                stopWatch.Reset();
            }

            builder.AppendLine("");
            Console.WriteLine(builder.ToString());
        }

        public async Task PerformOperationAsync<T>(Func<Task<IEnumerable<T>>> Operation, int? expectedItems, string operationName = "")
        {

            var stopWatch = new Stopwatch();

            var builder = new StringBuilder();
            builder.Append($"\tOperation: {operationName}");
            builder.AppendLine("");

            for (int i = 0; i < 1000; i++)
            {
                stopWatch.Start();
                IEnumerable<T> data = null;
                bool fail = false;
                try
                {
                    data = await Operation.Invoke();
                }
                catch (Exception) { fail = true; }
                var elapsed = stopWatch.ElapsedMilliseconds;
                builder.AppendLine("\t" + WriteTestResult(i + 1, _databaseName, elapsed, default, data?.Count() ?? 0, expectedItems ?? -1, fail));

                stopWatch.Reset();
            }

            builder.AppendLine("");
            Console.WriteLine(builder.ToString());
        }

        public static string WriteTestResult(int iteration, string database, long ElapsedMilliseconds, string additional, int rows = -1, int expectedRows = -1, bool forceFail = false)
        {
            string expectedResult;
            if (expectedRows > -1 && rows > -1)
                expectedResult = $" (expected: {expectedRows}) | RESULT: {(rows == expectedRows && !forceFail ? "OK" : "ERROR")}";
            else
                expectedResult = " | RESULT: OK";

            additional = !string.IsNullOrEmpty(additional) ? $" -> {additional}" : "" + expectedResult;
            return $"({database}) Iteration {iteration.ToString().PadLeft(3, '0')} Elapse ml: {ElapsedMilliseconds.ToString().PadLeft(8, '0')}{additional}";
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
