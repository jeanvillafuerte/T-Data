using Thomas.Cache.MemoryCache;
using Thomas.Database;
using Thomas.Database.Configuration;

namespace Thomas.Cache
{
    public static class CachedDbFactory
    {
        public static ICachedDatabase GetDbContext(string signature, bool buffered = true)
        {
            var config = DbConfigurationFactory.Get(signature);
            var database = new DbBase(in config, in buffered);
            return new CachedDatabase(DbDataCache.Instance, database, config.SQLValues);
        }
    }
}
