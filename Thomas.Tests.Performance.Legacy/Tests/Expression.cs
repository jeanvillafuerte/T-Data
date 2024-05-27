using System.Threading.Tasks;
using Thomas.Cache;
using Thomas.Database;
using Thomas.Database.Core.FluentApi;
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
            LoadTableBuilder(tableName);
            PerformOperation<Person>(() => DbFactory.GetDbContext(db).ToList<Person>(x => x.Id > 0), expectedItems, "ToList<> Expression");
        }

        public async Task ExecuteAsync(string db, string tableName, int expectedItems = 0)
        {
            LoadTableBuilder(tableName);
            await PerformOperationAsync(() => DbFactory.GetDbContext(db).ToListAsync<Person>(x => x.Id > 0), expectedItems, "ToListAsync<> Expression");
        }

        public void ExecuteCachedDatabase(string db, string tableName, int expectedItems = 0)
        {
            LoadTableBuilder(tableName);
            PerformOperation(() => CachedDbFactory.GetDbContext(db).ToList<Person>(x => x.Id > 0), expectedItems, "ToList<> Expression (cached)");
        }

        private void LoadTableBuilder(string tableName)
        {
            //custom table configuration made possible querying using expression 
            var builder = new TableBuilder();
            DbTable dbTable = builder.Configure<Person>(x => x.Id);
            dbTable.AddFieldsAsColumns<Person>().DbName(tableName);
            DbFactory.AddDbBuilder(builder);
        }
    }
}
