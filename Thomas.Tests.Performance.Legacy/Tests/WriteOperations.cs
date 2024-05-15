using Thomas.Database;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Legacy.Tests
{
    internal class WriteOperations(string databaseName) : TestCase(databaseName)
    {
        public void Execute(string db, string tableName, int expectedItems = 0)
        {
            PerformOperation(() => DbFactory.GetDbContext(db).Add(new Person { FirstName = "Jhon", LastName = "Doe", Country = "Peru", Salary = 9000, State = true, UserName = "JDoe" }), null, "Insert entity");
            PerformOperation(() => DbFactory.GetDbContext(db).Add<Person, int>(new Person { FirstName = "Jean", LastName = "Villafuerte", Country = "Peru", Salary = 9000, State = true, UserName = "Jean" }), null, "Insert  entity return ID");
            PerformOperation(() => DbFactory.GetDbContext(db).Update(new Person { FirstName = "John", LastName = "Doe", Country = "Usa", Salary = 9000, State = true, UserName = "JDoe2", Id = 1 }), null, "Update entity");
            PerformOperation(() => DbFactory.GetDbContext(db).Delete<Person>(new Person { Id = 1 }), null, "Delete entity");
        }
    }
}
