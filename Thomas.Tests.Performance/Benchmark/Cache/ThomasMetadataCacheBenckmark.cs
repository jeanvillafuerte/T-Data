using BenchmarkDotNet.Attributes;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Thomas.Database.Cache;
using Thomas.Database.Core.Converters;

namespace Thomas.Tests.Performance.Benchmark.Cache
{
    [Description("Thomas cache")]
    [ThreadingDiagnoser]
    [MemoryDiagnoser]
    public class ThomasMetadataCacheBenckmark
    {
        private PropertyInfo _propertyType;
        private readonly CultureInfo _culture;
        private readonly ITypeConversionStrategy[] _converters;

        public ThomasMetadataCacheBenckmark()
        {
            _culture = new CultureInfo("en-US");
            _converters = new ITypeConversionStrategy[0];
            _propertyType = typeof(Person).GetProperty("Name");
        }

        [Benchmark(Description = "SetCacheValue")]
        public void SetCacheValue()
        {
            var value = new MetadataPropertyInfo[0];
            CacheResultInfo.Set("key", value);
        }

        [Benchmark(Description = "GetCacheValue")]
        public bool GetCacheValue()
        {
            MetadataPropertyInfo[] data = null;
            return CacheResultInfo.TryGet("key", ref data);
        }

        [Benchmark(Description = "SetValue")]
        public void SetValue()
        {
            var data = new MetadataPropertyInfo(_propertyType);
            var person = new Person();
            var name = "Thomas";
            data.SetValue(person, name, in _culture, in _converters);
        }

        [Benchmark(Description = "GetValue")]
        public object GetValue()
        {
            var data = new MetadataPropertyInfo(_propertyType);
            var person = new Person() { Name = "Thomas" };
            return data.GetValue(person);
        }
    }

    class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
