using Microsoft.Data.SqlClient;
using System.ComponentModel;
using BenchmarkDotNet.Attributes;
using Thomas.Tests.Performance.Entities;
using Dapper;
using System.Linq;
using BenchmarkDotNet.Engines;
using System.Collections.Generic;

namespace Thomas.Tests.Performance.Benchmark.Others
{
    [Description("Dapper")]
    [ThreadingDiagnoser]
    [MemoryDiagnoser]
    public class DapperBenckmark : BenckmarkBase
    {
        //private SqlConnection _connection;
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

            IEnumerable<Person> list = _connection.Query<Person>($"select * from {TableName} where Id > @Id", new { Id = 1 }, buffered: false);
            _connection.Close();

            list.Consume(consumer);
        }

        //[Benchmark(Description = "Query<dynamic> (buffered)")]
        //public dynamic QueryBufferedDynamic()
        //{
        //    return _connection.Query($"select * from {TableName} where Id = @Id", new { Id = 1 }, buffered: true).First();
        //}

        //[Benchmark(Description = "Query<T> (unbuffered)")]
        //public Person QueryUnbuffered()
        //{
        //    return _connection.Query<Person>($"select * from {TableName} where Id = @Id", new { Id = 1 }, buffered: false).First();
        //}

        //[Benchmark(Description = "Query<T> List (unbuffered)")]
        //public void QueryListUnbuffered()
        //{
        //    var list = _connection.Query<Person>($"select * from {TableName} where Id = @Id", new { Id = 1 }, buffered: false);
        //    list.Consume(consumer);
        //}
    }
}
