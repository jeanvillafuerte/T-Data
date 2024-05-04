using Thomas.Database;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Legacy.Tests
{
    internal class WriteOperations(string databaseName) : TestCase(databaseName)
    {
        public void Execute(IDatabase service, string tableName, int expectedItems = 0)
        {
            PerformOperation(() => service.Add(new Person { FirstName = "Jhon", LastName = "Doe", Country = "Peru", Salary = 9000, State = true, UserName = "JDoe" }), null, "Insert entity");
            PerformOperation(() => service.Add<Person, int>(new Person { FirstName = "Jean", LastName = "Villafuerte", Country = "Peru", Salary = 9000, State = true, UserName = "Jean" }), null, "Insert  entity return ID");
            PerformOperation(() => service.Update(new Person { FirstName = "John", LastName = "Doe", Country = "Usa", Salary = 9000, State = true, UserName = "JDoe2", Id = 1 }), null, "Update entity");
            PerformOperation(() => service.Delete<Person>(new Person { Id = 1 }), null, "Delete entity");
        }
    }
}
