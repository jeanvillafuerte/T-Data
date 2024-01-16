using Thomas.Database.Configuration;

namespace Thomas.Database
{
    public static class DbFactory
    {
        public static IDatabase CreateDbContext(string signature)
        {
            var (config, provider) = DbConfigurationFactory.Get(signature);
            return new DatabaseBase(provider, config);
        }
    }
}
