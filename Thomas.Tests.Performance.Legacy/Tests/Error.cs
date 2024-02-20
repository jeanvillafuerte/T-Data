using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Thomas.Cache;
using Thomas.Database;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Legacy.Tests
{
    public class Error : TestCase
    {
        public Error(string databaseName) : base(databaseName)
        {
        }

        public void Execute(IDatabase service, string databaseName, string tableName, int expectedItems = 0)
        {
            PerformOperation(() => service.ToListOp<Person>($@"SELECT UserName2 FROM {tableName}"), null, "ToListOp<> error resilient");
            PerformOperation(() => service.ToSingleOp<Person>($@"SELECT UserName2 FROM {tableName}"), null, "ToSingleOp<> error resilient");
        }

        public async Task ExecuteAsync(IDatabase service, string databaseName, string tableName, int expectedItems = 0)
        {
            await PerformOperationAsync(() => service.ToListOpAsync<Person>($@"SELECT UserName2 FROM {tableName}", null, CancellationToken.None), null, "ToListOpAsync<> error resilient");
            await PerformOperationAsync(() => service.ToSingleOpAsync<Person>($@"SELECT UserName2 FROM {tableName}", null, CancellationToken.None), null, "ToSingleOpAsync<> error resilient");
        }

        public void ExecuteCachedDatabase(ICachedDatabase database, string databaseName, string tableName, int expectedItems = 0)
        {
            
        }
    }
}
