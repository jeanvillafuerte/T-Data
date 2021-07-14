using System;
namespace Thomas.Tests.Performance.Entities
{
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
}