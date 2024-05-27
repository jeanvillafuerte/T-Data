using System;
using System.Data;
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
        public Guid? UniqueId { get; set; }
        public bool State { get; set; }
        public DateTime? LastUpdate { get; set; }
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

        [DbParameter(direction: ParameterDirection.Output, size: 25)]
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

        [DbParameter(direction: ParameterDirection.Output)]
        public int Total { get; set; }
    }

    public class PersonClass
    {
        public int Id { get; set; } // 4 bytes
        public string UserName { get; set; } // 25 bytes
        public string FirstName { get; set; }  // 500 bytes
        public string LastName { get; set; } // 500 bytes
        public DateTime BirthDate { get; set; } // 3 bytes
        public short Age { get; set; } // 2 bytes
        public string Occupation { get; set; } // 300 bytes
        public string Country { get; set; } // 240 bytes
        public decimal Salary { get; set; } // 13 bytes
        public Guid UniqueId { get; set; } // 16 bytes
        public bool State { get; set; } // 1 bytes
        public DateTime? LastUpdate { get; set; } // 3 bytes
    }

#if NETCOREAPP
    public record PersonRecord
    {
        public int Id { get; set; } // 4 bytes
        public string UserName { get; set; } // 25 bytes
        public string FirstName { get; set; }  // 500 bytes
        public string LastName { get; set; } // 500 bytes
        public DateTime BirthDate { get; set; } // 3 bytes
        public short Age { get; set; } // 2 bytes
        public string Occupation { get; set; } // 300 bytes
        public string Country { get; set; } // 240 bytes
        public decimal Salary { get; set; } // 13 bytes
        public Guid UniqueId { get; set; } // 16 bytes
        public bool State { get; set; } // 1 bytes
        public DateTime? LastUpdate { get; set; } // 3 bytes
    }

    public record PersonReadonlyRecord
        (int Id,
        string UserName,
        string FirstName,
        string LastName,
        DateTime BirthDate,
        short Age,
        string Occupation,
        string Country,
        decimal Salary,
        Guid UniqueId,
        bool State,
        DateTime? LastUpdate);
#endif
}
