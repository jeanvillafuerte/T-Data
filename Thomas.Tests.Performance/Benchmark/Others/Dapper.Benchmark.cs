using System.ComponentModel;
using System.Linq;
using Microsoft.Data.SqlClient;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Dapper;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Benchmark.Others
{
    [Description("Dapper")]
    [ThreadingDiagnoser]
    [MemoryDiagnoser]
    public class DapperBenckmark : BenckmarkBase
    {
        private readonly Consumer consumer = new Consumer();

        [GlobalSetup]
        public void Setup()
        {
            Start();
        }

        [Benchmark(Description = "Query<T> (unbuffered)")]
        public void QueryBuffered()
        {
            var _connection = new SqlConnection(StringConnection);
            _connection.Open();

            var list = _connection.Query<Person>($"select * from {TableName} where Id = @Id", new { Id = 1 }, buffered: false);
            _connection.Close();

            list.Consume(consumer);
        }

        [Benchmark(Description = "Query<dynamic> (buffered)")]
        public dynamic QueryBufferedDynamic()
        {
            var _connection = new SqlConnection(StringConnection);
            var item = _connection.Query($"select * from {TableName} where Id = @Id", new { Id = 1 }, buffered: true).First();
            _connection.Close();
            return item;
        }

        [Benchmark(Description = "Query<T> (unbuffered)")]
        public Person QueryUnbuffered()
        {
            var _connection = new SqlConnection(StringConnection);
            var item = _connection.Query<Person>($"select * from {TableName} where Id = @Id", new { Id = 1 }, buffered: false).First();
            _connection.Close();
            return item;
        }

        [Benchmark(Description = "Query<T> List (unbuffered)")]
        public void QueryListUnbuffered()
        {
            var _connection = new SqlConnection(StringConnection);
            var list = _connection.Query<Person>($"select * from {TableName} where Id = @Id", new { Id = 1 }, buffered: false);
            _connection.Close();
            list.Consume(consumer);
        }
    }
}
