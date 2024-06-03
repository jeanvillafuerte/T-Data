using System.Collections.Concurrent;

namespace Thomas.Cache.MemoryCache
{
    internal sealed class DbDataCache : IDbDataCache
    {
        private static DbDataCache? instance;
        public static DbDataCache Instance
        {
            get
            {
                instance ??= new DbDataCache();
                return instance;
            }
        }

        private DbDataCache() { }

        private static ConcurrentDictionary<int, IQueryResult> CacheObject { get; set; } = new ConcurrentDictionary<int, IQueryResult>();
        
        public void AddOrUpdate(int key, IQueryResult data)
        {
            CacheObject.AddOrUpdate(key, data, (k, v) => data);
        }

        public static bool TryGetValue(int key, out IQueryResult? data) => CacheObject.TryGetValue(key, out data);

        public bool TryGet<T>(int key, out QueryResult<T>? result)
        {
            if (CacheObject.TryGetValue(key, out IQueryResult? data))
            {
                if (data is QueryResult<T> convertedValue)
                {
                    result = convertedValue;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public void Clear(int hash)
        {
            CacheObject.TryRemove(hash, out var _);
        }

        public void Clear()
        {
            CacheObject.Clear();
        }
    }

}

