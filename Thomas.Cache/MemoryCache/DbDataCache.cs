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
                instance ??= new DbDataCache();
                return instance;
            }
        }

        private DbDataCache() { }

        private static ConcurrentDictionary<int, IQueryResult> CacheObject  = new ConcurrentDictionary<int, IQueryResult>();
        
        public void AddOrUpdate(in int key, IQueryResult result)
        {
            CacheObject.AddOrUpdate(key, result, (k, v) => result);
        }

        public static bool TryGetValue(in int key, out IQueryResult data) => CacheObject.TryGetValue(key, out data);

        public bool TryGet<T>(in int key, out QueryResult<T> result)
        {
            if (CacheObject.TryGetValue(key, out IQueryResult data) && data is QueryResult<T> convertedValue)
            {
                result = convertedValue;
                return true;
            }

            result = null;
            return false;
        }

        public void Clear(in int hash)
        {
            CacheObject.TryRemove(hash, out var _);
        }

        public void Clear()
        {
            CacheObject.Clear();
        }
    }

}

