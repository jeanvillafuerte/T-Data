using System;
using Thomas.Database;
using Thomas.Database.Attributes;

namespace Thomas.Tests.Performance.Entities
{
    [Serializable]
    public class Person
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime BirthDate { get; set; }
        public short Age { get; set; }
        public string Occupation { get; set; }
        public string Country { get; set; }
        public decimal Salary { get; set; }
        public Guid UniqueId { get; set; }
        public bool State { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    [Serializable]
    public class PersonWithNullables
    {
        public int? Id { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? BirthDate { get; set; }
        public short Age { get; set; }
        public string Occupation { get; set; }
        public string Country { get; set; }
        public decimal Salary { get; set; }
        public Guid? UniqueId { get; set; }
        public bool State { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    [Serializable]
    public class SearchTerm
    {
        public SearchTerm(int id)
        {
            Id = id;
        }

        public int Id { get; set; }

        [ParameterDirection(ParamDirection.Output)]
        [ParameterSize(25)]
        public string UserName { get; set; }
    }

    [Serializable]
    public class ListResult
    {
        public ListResult(int age)
        {
            Age = age;
        }

        public int Age { get; set; }

        [ParameterDirection(ParamDirection.Output)]
        public int Total { get; set; }
    }
}