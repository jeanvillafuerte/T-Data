using System.Collections.Concurrent;

namespace Thomas.Cache.MemoryCache
{
    internal sealed class DbParameterCache : IDbParameterCache
    {
        private static DbParameterCache instance;
        public static DbParameterCache Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DbParameterCache();
                }
                return instance;
            }
        }

        private static ConcurrentDictionary<string, object> CacheParams { get; set; } = new ConcurrentDictionary<string, object>();

        public void AddOrUpdate(string hash, object result) => CacheParams.AddOrUpdate(hash, result, (k, v) => result);

        public void Release(string hash) => CacheParams.TryRemove(hash, out var _);

        public bool TryGet(string hash, out object? result) => CacheParams.TryGetValue(hash, out result);

        public void Clear() => CacheParams.Clear();
    }
}
