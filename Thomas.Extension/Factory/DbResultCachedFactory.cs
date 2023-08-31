using Thomas.Cache.Manager;
using Thomas.Database;
using Thomas.Database.Configuration;
using Thomas.Database.Exceptions;

namespace Thomas.Cache.Factory
{
    internal class DbResultCachedFactory : IDbResultCachedFactory
    {
        private readonly IDbConfigurationFactory _configuration;

        public DbResultCachedFactory(IDbConfigurationFactory configuration)
        {
            _configuration = configuration;
        }

        public ICachedDatabase CreateDbContext(string signature)
        {
            var (config, provider) = _configuration.Get(signature);

            if (config == null)
                throw new DbConfigurationNotFoundException($"Db configuration {signature} cannot found.");

            var database = new DatabaseBase(provider, config);

            IDbCacheManager cacheManager = config.UseCompressedCacheStrategy ? DbCompressedCacheManager.Instance : (IDbCacheManager)DbCacheManager.Instance;

            return new CachedDatabase(cacheManager, database, config.Signature, new System.Globalization.CultureInfo(config.Culture));
        }
    }
}
