using System;
using Thomas.Database.Configuration;
using Thomas.Database.Exceptions;

namespace Thomas.Database.Strategy.Factory
{
    public interface IDbFactory
    {
        IDatabase CreateDbContext(string signature);
    }

    internal class DbFactory : IDbFactory
    {
        private readonly IDbConfigurationFactory _configuration;

        public DbFactory(IDbConfigurationFactory configuration)
        {
            _configuration = configuration;
        }

        public IDatabase CreateDbContext(string signature)
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

            return new DatabaseBase(provider, strategy, config);
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
