using System.Data;
using TData.Attributes;

namespace TData.Tests
{
    class FilterSalary
    {
        [DbParameter(name: "ss", precision: 10, scale: 2)]
        public decimal SalaryStar { get; set; }

        [DbParameter(name: "se", precision: 10, scale: 2)]
        public decimal SalaryEnd { get; set; }

        [DbParameter(name: "t", direction: ParameterDirection.Output)]
        public int Total { get; set; }
    }

    record UserType(int Id, string Name);
    record User(int Id, int UserTypeId, string Name, bool State, decimal Salary, DateTime Birthday, Guid UserCode, byte[]? Icon);
    record UserNullableRecord(int Id, int UserTypeId, string Name, bool? State, decimal? Salary, DateTime? Birthday, Guid? UserCode, byte[]? Icon);
    class UserNullableClass { public int Id { get; set; } public int UserTypeId { get; set; } public string Name { get; set; } public bool? State { get; set; } public decimal? Salary { get; set; } public DateTime? Birthday { get; set; } public Guid? UserCode { get; set; } public byte[]? Icon { get; set; } }
    record SimpleTimeSpanRecord(TimeSpan Value);
    record SimpleGuidRecord(Guid Value);

    class Book
    {
        public int Id { get; set; }
        public string Content { get; set; }

        public Book(string content)
        {
            Content = content;
        }
    }
}
