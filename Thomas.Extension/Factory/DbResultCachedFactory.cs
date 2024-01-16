using Thomas.Cache.MemoryCache;
using Thomas.Database;
using Thomas.Database.Configuration;

namespace Thomas.Cache.Factory
{
    public static class DbResultCachedFactory
    {
        public static ICachedDatabase CreateDbContext(string signature)
        {
            var (config, provider) = DbConfigurationFactory.Get(signature);
            var database = new DatabaseBase(provider, config);
            return new CachedDatabase(DbDataCache.Instance, database, config.CultureInfo);
        }
    }
}
