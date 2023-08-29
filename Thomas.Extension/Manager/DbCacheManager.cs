using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Thomas.Cache.Manager
{
    public sealed class DbCacheManager : IDbCacheManager
    {
        private static DbCacheManager instance;
        public static DbCacheManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DbCacheManager();
                }
                return instance;
            }
        }

        private DbCacheManager() { }

        private static ConcurrentDictionary<uint, object> CacheObject { get; set; } = new ConcurrentDictionary<uint, object>();

        public void Add<T>(uint hash, IEnumerable<T> result) where T : class, new()  => CacheObject.TryAdd(hash, result);

        public bool TryGet<T>(uint hash, out T result) where T : class, new()
        {
            if (CacheObject.TryGetValue(hash, out object? data))
            {
                if (data is T convertedValue)
                {
                    result = convertedValue;
                    return true;
                }
            }

            result = new T();
            return false;
        }

        public bool TryGet<T>(uint hash, out IEnumerable<T> result) where T : class, new()
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

        public void Release(uint hash) => CacheObject.TryRemove(hash, out var _);

        public void Clear() => CacheObject.Clear();
    }

}

