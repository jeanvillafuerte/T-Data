using TData;
using TData.Tests.Performance.Entities;

namespace TData.Tests.Performance.Legacy.Tests
{
    internal class WriteOperations(string databaseName) : TestCase(databaseName)
    {
        public void Execute(string db, string tableName, int expectedItems = 0)
        {
            PerformOperation(() => DbHub.Use(db).Insert(new Person { FirstName = "Jhon", LastName = "Doe", Country = "Peru", Salary = 9000, State = true, UserName = "JDoe" }), "Insert");
            PerformOperation(() => DbHub.Use(db).Insert<Person, int>(new Person { FirstName = "Jean", LastName = "Villafuerte", Country = "Peru", Salary = 9000, State = true, UserName = "Jean" }), "Insert return ID");
            PerformOperation(() => DbHub.Use(db).Update(new Person { FirstName = "John", LastName = "Doe", Country = "Usa", Salary = 9000, State = true, UserName = "JDoe2", Id = 1 }), "Update");
            PerformOperation(() => DbHub.Use(db).Delete<Person>(new Person { Id = 1 }), "Delete");
        }
    }
}
