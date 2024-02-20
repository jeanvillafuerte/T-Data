using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Thomas.Tests.Performance")]

namespace Thomas.Database.Cache
{
    public sealed class CacheResultInfo
    {
        private static CacheResultInfo instance;
        private static readonly ConcurrentDictionary<string, MetadataPropertyInfo[]> ResponseTypes = new ConcurrentDictionary<string, MetadataPropertyInfo[]>(Environment.ProcessorCount * 2, 100);

        public static CacheResultInfo Instance
        {
            get
            {
                instance ??= new CacheResultInfo();
                return instance;
            }
        }

        private CacheResultInfo() { }

        internal static void Set(in string key, in MetadataPropertyInfo[] value) => ResponseTypes.TryAdd(key, value);
        internal static bool TryGet(in string key, ref MetadataPropertyInfo[] meta)
        {
            var result = ResponseTypes.TryGetValue(key, out var parameters);

            if (!result)
            {
                return false;
            }

            meta = parameters;
            return true;
        }
        public void Clear() => ResponseTypes.Clear();
    }
}
