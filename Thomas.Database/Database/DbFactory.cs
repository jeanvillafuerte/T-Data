using Thomas.Database.Configuration;
using Thomas.Database.Core.FluentApi;

namespace Thomas.Database
{
    public static class DbFactory
    {
        public static IDatabase GetDbContext(string signature, bool buffered = true)
        {
            var config = DbConfigurationFactory.Get(in signature);
            if (config == null)
                throw new System.Exception("Database configuration not found");

            return new DbBase(in config, in buffered);
        }

        public static void AddDbBuilder(TableBuilder builder) => DbConfigurationFactory.AddTableBuilder(in builder);
    }
}
