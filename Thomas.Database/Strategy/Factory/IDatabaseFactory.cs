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

            return new DatabaseBase(provider, config);
        }

    }
}
