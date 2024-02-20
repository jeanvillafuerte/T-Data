using System.Collections.Concurrent;

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

        private static ConcurrentDictionary<string, IQueryResult> CacheObject { get; set; } = new();

        public void AddOrUpdate(string hash, IQueryResult data) => CacheObject.AddOrUpdate(hash, data, (k, v) => data);

        public bool TryGetValue(string hash, out IQueryResult? data)
        {
            return CacheObject.TryGetValue(hash, out data);
        }

        public bool TryGet<T>(string hash, out QueryResult<T>? result)
        {
            if (CacheObject.TryGetValue(hash, out IQueryResult? data))
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

        public void Release(string hash) => CacheObject.TryRemove(hash, out var _);

        public void Release()
        {
            CacheObject.Clear();
        }
    }

}

