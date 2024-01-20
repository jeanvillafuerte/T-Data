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

        private static ConcurrentDictionary<string, IDictionaryDbQueryItem> CacheObject { get; set; } = new ConcurrentDictionary<string, IDictionaryDbQueryItem>();

        public void AddOrUpdate(string hash, IDictionaryDbQueryItem data) => CacheObject.AddOrUpdate(hash, data, (k, v) => data);

        public bool TryGetNative(string hash, out IDictionaryDbQueryItem? data)
        {
            return CacheObject.TryGetValue(hash, out data);
        }

        public bool TryGet<T>(string hash, out DictionaryDbQueryItem<T>? result)
        {
            if (CacheObject.TryGetValue(hash, out IDictionaryDbQueryItem? data))
            {
                if (data is DictionaryDbQueryItem<T> convertedValue)
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

