using Thomas.Database;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Legacy.Tests
{
    internal class WriteOperations : TestCase
    {
        public WriteOperations(string databaseName) : base(databaseName)
        {
        }

        public void Execute(IDatabase service, string tableName, int expectedItems = 0)
        {
            PerformOperation(() => service.Add(new Person { FirstName = "Jhon", LastName = "Doe", Country = "Peru", Salary = 9000, State = true, UserName = "JDoe" }), null, "Insert");
            PerformOperation(() => service.Add<Person, int>(new Person { FirstName = "Jean", LastName = "Villafuerte", Country = "Peru", Salary = 9000, State = true, UserName = "Jean" }), null, "Insert return ID");
            PerformOperation(() => service.Update(new Person { FirstName = "John", LastName = "Doe", Country = "Usa", Salary = 9000, State = true, UserName = "JDoe2" }, x => x.Id == 1), null, "Update");
            PerformOperation(() => service.Delete<Person>(x => x.Id == 1), null, "Insert return ID");
        }
    }
}
