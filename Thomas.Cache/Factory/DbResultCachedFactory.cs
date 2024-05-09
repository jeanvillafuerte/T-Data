using Thomas.Cache.MemoryCache;
using Thomas.Database;
using Thomas.Database.Configuration;

namespace Thomas.Cache.Factory
{
    public static class CachedDbFactory
    {
        public static ICachedDatabase CreateDbContext(string signature)
        {
            var config = DbConfigurationFactory.Get(signature);
            var database = new DbBase(config);
            return new CachedDatabase(DbDataCache.Instance, database, config.SQLValues);
        }
    }
}
