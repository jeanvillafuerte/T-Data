using Thomas.Database.Configuration;
using Thomas.Database.Core.FluentApi;

namespace Thomas.Database
{
    public static class DbHub
    {
        public static IDatabase GetDefaultDb(bool buffered = true)
        {
            var config = DbConfig.Get();
            return new DbBase(in config, in buffered);
        }

        public static IDatabase Use(string signature, bool buffered = true)
        {
            var config = DbConfig.Get(in signature);
            if (config == null)
                throw new System.Exception("Database configuration not found");

            return new DbBase(in config, in buffered);
        }

        public static void AddDbBuilder(TableBuilder builder) => DbConfig.AddTableBuilder(in builder);
    }
}
