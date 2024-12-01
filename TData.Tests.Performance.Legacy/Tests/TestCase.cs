using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TData.Tests.Performance.Legacy.Tests
{
    public abstract class TestCase(string databaseName)
    {
        protected readonly string _databaseName = databaseName;

        public void PerformOperation(Action operation, string operationName)
        {
            var bag = new ConcurrentBag<long>();
            var errorCount = 0;
            Parallel.For(1, 10, (i) =>
            {
                var stopWatch = new Stopwatch();
                for (int j = 0; j < 100; j++)
                {
                    try
                    {
                        operation.Invoke();
                        bag.Add(stopWatch.ElapsedMilliseconds);
                    }
                    catch (Exception)
                    {
                        bag.Add(stopWatch.ElapsedMilliseconds);
                        Interlocked.Increment(ref errorCount);
                    }

                    stopWatch.Reset();
                }
            });

            var avg =  bag.IsEmpty ? 0 : Math.Round(bag.Average(), 2);
            Console.WriteLine($"\tOperation: {operationName} ({_databaseName}) Elapse ml avg: {avg.ToString()}{(errorCount == 0 ? "" : $"Errors: {errorCount}")}");
        }

        public void PerformOperation<T>(Func<T> operation, string operationName)
        {
            var bag = new ConcurrentBag<long>();
            var errorCount = 0;
            Parallel.For(1, 10, (i) =>
            {
                var stopWatch = new Stopwatch();
                for (int j = 0; j < 100; j++)
                {
                    T result = default;
                    var error = false;
                    try
                    {
                        stopWatch.Start();
                        result = operation.Invoke();
                        bag.Add(stopWatch.ElapsedMilliseconds);
                    }
                    catch (Exception)
                    {
                        Interlocked.Increment(ref errorCount);
                        bag.Add(stopWatch.ElapsedMilliseconds);
                        error = true;
                    }
                    finally
                    {
                        if (!error && result == null)
                            Interlocked.Increment(ref errorCount);

                        stopWatch.Reset();
                    }
                }
            });

            var avg =  bag.IsEmpty ? 0 : Math.Round(bag.Average(), 2);
            Console.WriteLine($"\tOperation: {operationName} ({_databaseName}) Elapse ml avg: {avg.ToString()} {(errorCount == 0 ? "" : $"Errors: {errorCount}")}");
        }

        public void PerformOperation<T>(Func<DbOpResult<List<T>>> operation, int? expectedItems, string operationName)
        {
            var bag = new ConcurrentBag<long>();
            var errorCount = 0;
            Parallel.For(1, 10, (i) =>
            {
                var stopWatch = new Stopwatch();
                var error = false;

                for (int j = 0; j < 100; j++)
                {
                    DbOpResult<List<T>> result = null;
                    try
                    {
                        stopWatch.Start();
                        result = operation.Invoke();
                        bag.Add(stopWatch.ElapsedMilliseconds);
                    }
                    catch (Exception)
                    {
                        Interlocked.Increment(ref errorCount);
                        bag.Add(stopWatch.ElapsedMilliseconds);
                        error = true;
                    }
                    finally
                    {
                        if (!error && (result == null || !result.Success || (expectedItems.HasValue && result.Result.Count != expectedItems.Value)))
                            Interlocked.Increment(ref errorCount);

                        stopWatch.Reset();
                    }
                }
            });

            var avg =  bag.IsEmpty ? 0 : Math.Round(bag.Average(), 2);
            Console.WriteLine($"\tOperation: {operationName} ({_databaseName}) Elapse ml avg: {avg.ToString()} {(errorCount == 0 ? "" : $"Errors: {errorCount}")}");
        }

        public void PerformOperation<T>(Func<List<T>> operation, int? expectedItems, string operationName)
        {
            var bag = new ConcurrentBag<long>();
            var errorCount = 0;

            Parallel.For(1, 10, (i) =>
            {
                var stopWatch = new Stopwatch();
                var error = false;

                for (int j = 0; j < 100; j++)
                {
                    List<T> items = null;
                    try
                    {
                        stopWatch.Start();
                        items = operation.Invoke();
                        bag.Add(stopWatch.ElapsedMilliseconds);
                    }
                    catch (Exception)
                    {
                        Interlocked.Increment(ref errorCount);
                        bag.Add(stopWatch.ElapsedMilliseconds);
                        error = true;
                    }
                    finally
                    {
                        if (!error && (items == null || (expectedItems.HasValue && items.Count != expectedItems.Value)))
                            Interlocked.Increment(ref errorCount);

                        stopWatch.Reset();
                    }
                }
            });
            

            var avg =  bag.IsEmpty ? 0 : Math.Round(bag.Average(), 2);
            Console.WriteLine($"\tOperation: {operationName} ({_databaseName}) Elapse ml avg: {avg.ToString()} {(errorCount == 0 ? "" : $"Errors: {errorCount}")}");
        }

        public void PerformOperationAsync<T>(Func<Task<T>> operation, string operationName, bool shouldFail = false)
        {
            var bag = new ConcurrentBag<long>();
            var errorCount = 0;

            Parallel.For(1, 10, async (i) =>
            {
                var stopWatch = new Stopwatch();
                var error = false;

                for (int j = 0; j < 100; j++)
                {
                    T result = default;
                    try
                    {
                        stopWatch.Start();
                        result = await operation.Invoke();
                        bag.Add(stopWatch.ElapsedMilliseconds);
                    }
                    catch (Exception)
                    {
                        if (!shouldFail)
                            Interlocked.Increment(ref errorCount);
                        bag.Add(stopWatch.ElapsedMilliseconds);
                        error = true;
                    }
                    finally
                    {
                        if (!error && !shouldFail && result == null)
                            Interlocked.Increment(ref errorCount);

                        stopWatch.Reset();
                    }
                }
            });

            var avg =  bag.IsEmpty ? 0 : Math.Round(bag.Average(), 2);
            Console.WriteLine($"\tOperation: {operationName} ({_databaseName}) Elapse ml avg: {avg.ToString()} {(errorCount == 0 ? "" : $"Errors: {errorCount}")}");
        }

        public void PerformOperationAsync<T>(Func<Task<List<T>>> operation, int expectedItems, string operationName)
        {
            var bag = new ConcurrentBag<long>();
            var errorCount = 0;

            Parallel.For(1, 10, async (i) =>
            {
                var stopWatch = new Stopwatch();
                var error = false;

                for (int j = 0; j < 100; j++)
                {
                    List<T> result = default;
                    try
                    {
                        stopWatch.Start();
                        result = await operation.Invoke();
                        bag.Add(stopWatch.ElapsedMilliseconds);
                    }
                    catch (Exception)
                    {
                        Interlocked.Increment(ref errorCount);
                        bag.Add(stopWatch.ElapsedMilliseconds);
                        error = true;
                    }
                    finally
                    {
                        if (!error && (result == null || result.Count != expectedItems))
                            Interlocked.Increment(ref errorCount);

                        stopWatch.Reset();
                    }
                }
            });

            var avg =  bag.IsEmpty ? 0 : Math.Round(bag.Average(), 2);
            Console.WriteLine($"\tOperation: {operationName} ({_databaseName}) Elapse ml avg: {avg.ToString()} {(errorCount == 0 ? "" : $"Errors: {errorCount}")}");
        }

    }
}
