using System;
using System.Collections.Concurrent;

namespace Thomas.Database.Cache
{
    internal class CacheQueryString
    {
        private static CacheQueryString instance;
        private static readonly ConcurrentDictionary<string, string> SqlTexts = new ConcurrentDictionary<string, string>(Environment.ProcessorCount * 2, 100);

        public static CacheQueryString Instance
        {
            get
            {
                instance ??= new CacheQueryString();
                return instance;
            }
        }

        private CacheQueryString() { }

        internal static void Set(in string key, in string value) => SqlTexts.TryAdd(key, value);
        internal static bool TryGet(in string key, out string meta) => SqlTexts.TryGetValue(key, out meta);
        public void Clear() => SqlTexts.Clear();

    }
}
