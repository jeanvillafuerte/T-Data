using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Thomas.Cache.MemoryCache
{
    internal sealed class DbDataCache : IDbDataCache
    {
        private static DbDataCache instance;
        public static DbDataCache Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DbDataCache();
                }
                return instance;
            }
        }

        private DbDataCache() { }

        private static ConcurrentDictionary<string, object> CacheObject { get; set; } = new ConcurrentDictionary<string, object>();

        public void AddOrUpdate<T>(string hash, IEnumerable<T> result) where T : class, new() => CacheObject.AddOrUpdate(hash, result, (k, v) => result);

        public bool TryGet<T>(string hash, out IEnumerable<T> result) where T : class, new()
        {
            if (CacheObject.TryGetValue(hash, out object? data))
            {
                if (data is IEnumerable<T> convertedValue)
                {
                    result = convertedValue;
                    return true;
                }
            }

            result = Enumerable.Empty<T>();
            return false;
        }

        public void Release(string hash) => CacheObject.TryRemove(hash, out var _);

        public void Clear()
        {
            CacheObject.Clear();
        }
    }

}

