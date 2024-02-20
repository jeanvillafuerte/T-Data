using Thomas.Database.Configuration;
using Thomas.Database.Core.FluentApi;

namespace Thomas.Database
{
    public static class DbFactory
    {
        public static IDatabase CreateDbContext(string signature)
        {
            var config = DbConfigurationFactory.Get(signature);

            if (config == null)
                throw new System.Exception("Database configuration not found");
            return new DatabaseBase(config);
        }

        public static void AddDbBuilder(TableBuilder builder) => DbConfigurationFactory.AddTableBuilder(builder);
    }
}
