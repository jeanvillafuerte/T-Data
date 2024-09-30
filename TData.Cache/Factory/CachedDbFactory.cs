using System;
using TData.Cache.MemoryCache;
using TData.Configuration;

namespace TData.Cache
{
    public static class CachedDbHub
    {
        public static ICachedDatabase Use(string signature, bool buffered = true)
        {
            var config = DbConfig.Get(signature);
            return new CachedDatabase(DbDataCache.Instance, new Lazy<IDatabase>(() => new DbBase(in config, in buffered)), config.SQLValues);
        }

        public static void Clear()
        {
            DbDataCache.Instance.Clear();
        }
    }
}
