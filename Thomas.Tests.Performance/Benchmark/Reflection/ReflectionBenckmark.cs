using BenchmarkDotNet.Attributes;
using System.ComponentModel;
using System.Reflection;
using Thomas.Tests.Performance.Entities;

namespace Thomas.Tests.Performance.Benchmark.Reflection
{
    [Description("Dotnet Reflection")]
    [ThreadingDiagnoser]
    [MemoryDiagnoser]
    public class ReflectionBenckmark
    {
        [Benchmark(Description = "GetProperties")]
        public PropertyInfo[] GetProperties()
        {
            var type = typeof(Person);
            return type.GetProperties();
        }

        [Benchmark(Description = "GetProperty")]
        public PropertyInfo GetProperty()
        {
            var type = typeof(Person);
            return type.GetProperty("UserName");
        }

        [Benchmark(Description = "GetValue")]
        public object GetValue()
        {
            var person = new Person() { UserName = "Jean Carlos" };
            return typeof(Person).GetProperty("UserName").GetValue(person);
        }

        [Benchmark(Description = "SetValue")]
        public void SetValue()
        {
            var person = new Person();
            var property = typeof(Person).GetProperty("UserName");
            property.SetValue(person, "Jean Carlos");
        }
    }
}
