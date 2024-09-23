using System;
using Thomas.Cache.MemoryCache;
using Thomas.Database;
using Thomas.Database.Configuration;

namespace Thomas.Cache
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
