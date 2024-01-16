
namespace Thomas.Database.SqlServer
{
    using Thomas.Database.Configuration;

    public static class SqlServerFactory
    {
        public static void AddDb(DbSettings options) => DbConfigurationFactory.Register(options, new SqlProvider(options));
    }
}
