using System;
using Thomas.Cache.Manager;
using Thomas.Database;
using Thomas.Database.Configuration;
using Thomas.Database.Exceptions;
using Thomas.Database.Strategy;

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

            int processors = GetMaxDegreeOfParallelism(config);

            JobStrategy strategy;

            if (processors == 1)
                strategy = new SimpleJobStrategy(config.Culture, 1);
            else
                strategy = new MultithreadJobStrategy(config.Culture, processors);

            var database = new DatabaseBase(provider, strategy, config);

            IDbCacheManager cacheManager = config.UseCompressedCacheStrategy ? DbCompressedCacheManager.Instance : (IDbCacheManager) DbCacheManager.Instance;

            return new CachedDatabase(cacheManager, database, config.Signature, new System.Globalization.CultureInfo(config.Culture));
        }

        internal static int GetMaxDegreeOfParallelism(ThomasDbStrategyOptions options)
        {
            if (options.MaxDegreeOfParallelism <= 1)
                return 1;
            else
                return options.MaxDegreeOfParallelism > Environment.ProcessorCount ? Environment.ProcessorCount : options.MaxDegreeOfParallelism;
        }
    }
}
