using System.ComponentModel;
using BenchmarkDotNet.Attributes;
using Thomas.Database.Cache.Metadata;

namespace Thomas.Tests.Performance.Benchmark.Cache
{
    [Description("Thomas cache")]
    [ThreadingDiagnoser]
    [MemoryDiagnoser]
    public class ThomasMetadataCacheBenckmark
    {
        [Benchmark(Description = "SetCacheValue")]
        public void SetCacheValue()
        {
            var value = new MetadataPropertyInfo[0];
            CacheResultInfo.Set("key", in value);
        }

        [Benchmark(Description = "GetCacheValue")]
        public bool GetCacheValue()
        {
            MetadataPropertyInfo[] data = null;
            return CacheResultInfo.TryGet("key", ref data);
        }
    }
}
