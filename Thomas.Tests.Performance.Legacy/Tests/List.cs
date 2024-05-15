using System.Threading;
using System.Threading.Tasks;
using Thomas.Cache.Factory;
using Thomas.Database;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Legacy.Tests
{
    public class List : TestCase
    {
        public List(string databaseName) : base(databaseName)
        {
        }

        public void Execute(string db, string tableName, int expectedItems = 0)
        {
            PerformOperation(() => DbFactory.GetDbContext(db).ToList<Person>($"SELECT * FROM {tableName} WHERE Id > @Id", new { Id = 0 }), expectedItems, "ToList<>");
            PerformOperation(() => DbFactory.GetDbContext(db).ToListOp<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}"), expectedItems, "ToListOp<>");
            PerformOperation(() => DbFactory.GetDbContext(db).ToList<Person>($@"get_{tableName}", new { age = 5 }), null, "ToList<> Store Procedure");
            PerformOperation(() => DbFactory.GetDbContext(db).ToListOp<Person>($@"get_{tableName}", new { age = 5 }), null, "ToList<> Store Procedure");
            PerformOperation(() =>
            {
                var st = new ListResult(age: 35);
                return DbFactory.GetDbContext(db).ToList<Person>("get_byAge", st);
            }, null, "ToList<> by SP");
        }

        public async Task ExecuteAsync(string db, string tableName, int expectedItems = 0)
        {
            var query = $"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}";
            await PerformOperationAsync(() => DbFactory.GetDbContext(db).ToListAsync<Person>(query, null, CancellationToken.None), expectedItems: expectedItems, operationName: "ToListAsync<>");
            await PerformOperationAsync(() => DbFactory.GetDbContext(db).ToListOpAsync<Person>(query, null, CancellationToken.None), null, "ToListOpAsync<>");
            await PerformOperationAsync(() => DbFactory.GetDbContext(db).ToListAsync<Person>($@"get_{tableName}", new { age = 5 }, CancellationToken.None), null, "ToListAsync<> Store Procedure");
            await PerformOperationAsync(() => DbFactory.GetDbContext(db).ToListOpAsync<Person>($@"get_{tableName}", new { age = 5 }, CancellationToken.None), null, "ToListOpAsync<> Store Procedure");
            await PerformOperationAsync(() =>
            {
                var st = new ListResult(age: 35);
                return DbFactory.GetDbContext(db).ToListOpAsync<Person>("get_byAge", st, CancellationToken.None);
            }, null, "ToListOpAsync<> Store Procedure 2");

            await PerformOperationAsync(() =>
            {
                CancellationTokenSource source = new CancellationTokenSource();
                source.CancelAfter(100);
                return DbFactory.GetDbContext(db).ToListOpAsync<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", null, source.Token);
            }, null, "ToListOpAsync<> Cancelled");
        }

        public void ExecuteCachedDatabase(string db, string tableName, int expectedItems = 0)
        {
            PerformOperation(() => CachedDbFactory.GetDbContext(db).ToList<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}"), expectedItems, "ToList<>");
            PerformOperation(() => CachedDbFactory.GetDbContext(db).ToList<Person>($@"SELECT UserName, FirstName, LastName, BirthDate, Age, Occupation, Country, Salary, UniqueId, [State], LastUpdate FROM {tableName}", null, refresh: true), expectedItems, "ToList<> (refresh)");
            PerformOperation(() => CachedDbFactory.GetDbContext(db).ToList<Person>($@"get_{tableName}", new { age = 5 }), null, "ToList<> By SP");
            PerformOperation(() =>
            {
                var st = new ListResult(age: 35);
                return CachedDbFactory.GetDbContext(db).ToList<Person>("get_byAge", st);
            }, null, "ToList<> by SP with output");

            PerformOperation(() =>
            {
                var st = new ListResult(age: 35);
                return CachedDbFactory.GetDbContext(db).ToList<Person>("get_byAge", st);
            }, null, "ToList<> (refresh) by SP with output");
        }
    }
}
