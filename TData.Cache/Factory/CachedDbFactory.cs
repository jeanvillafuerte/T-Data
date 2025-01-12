using System;
using System.Collections.Concurrent;
using TData.Cache.MemoryCache;
using TData.Configuration;

namespace TData.Cache
{
    public static class CachedDbHub
    {
        internal static readonly ConcurrentDictionary<string, IDbDataCache> CacheDbDictionary = new ConcurrentDictionary<string, IDbDataCache>();
        public static ICachedDatabase Use(string signature, bool buffered = true)
        {
            var config = DbConfig.Get(signature);
            if (CacheDbDictionary.TryGetValue(signature, out var cacheDb))
            {
                return new CachedDatabase(cacheDb, new Lazy<IDatabase>(() => new DbBase(in config, in buffered)), config.SQLValues, cacheDb.TTL);
            }

            throw new Exception("Invalid Signature");
        }

        public static void Clear()
        {
            CacheDbDictionary.Clear();
        }
    }
}
