using System.ComponentModel;
using System.Linq;
using Microsoft.Data.SqlClient;
using Dapper;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
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

        [Benchmark(Description = "QuerySingle<T>")]
        public Person QuerySingle()
        {
            using var _connection = new SqlConnection(StringConnection);
            return _connection.QuerySingle<Person>($"select * from {TableName} where Id = @Id", new { Id = 1 });
        }

        [Benchmark(Description = "Query<T> (buffered)")]
        public void QueryBuffered()
        {
            using var _connection = new SqlConnection(StringConnection);
            _connection.Query<Person>($"SELECT * FROM {TableName}").Consume(consumer);
        }

        [Benchmark(Description = "Query<dynamic> (buffered)")]
        public dynamic QueryBufferedDynamic()
        {
            var _connection = new SqlConnection(StringConnection);
            var item = _connection.Query($"select * from {TableName} where Id > @Id", new { Id = 1 }, buffered: true).First();
            _connection.Close();
            return item;
        }

        [Benchmark(Description = "Query<T> (unbuffered)")]
        public void QueryListUnbuffered()
        {
            using var _connection = new SqlConnection(StringConnection);
            _connection.Query<Person>($"select * from {TableName} where Id > @Id", new { Id = 0 }, buffered: false).Consume(consumer);
        }
    }
}
