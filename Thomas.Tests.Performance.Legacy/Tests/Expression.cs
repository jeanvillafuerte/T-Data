using System.Threading.Tasks;
using Thomas.Cache;
using Thomas.Database;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Legacy.Tests
{
    public class Expression : TestCase
    {
        public Expression(string databaseName) : base(databaseName)
        {
        }

        public void Execute(string db, string tableName, int expectedItems = 0)
        {
            PerformOperation<Person>(() => DbHub.Use(db).FetchList<Person>(x => x.Id > 0), expectedItems, "FetchList<> Expression");
        }

        public async Task ExecuteAsync(string db, string tableName, int expectedItems = 0)
        {
            await PerformOperationAsync(() => DbHub.Use(db).FetchListAsync<Person>(x => x.Id > 0), expectedItems, "FetchListAsync<> Expression");
        }

        public void ExecuteCachedDatabase(string db, string tableName, int expectedItems = 0)
        {
            PerformOperation(() => CachedDbHub.Use(db).FetchList<Person>(x => x.Id > 0), expectedItems, "FetchList<> Expression (cached)");
        }

    }
}
