using Microsoft.Extensions.DependencyInjection;

namespace Thomas.Database.SqlServer
{
    using Thomas.Database.Configuration;

    public static class IServiceCollectionExtension
    {
        public static IServiceCollection AddSqlDatabase(this IServiceCollection services, ThomasDbStrategyOptions options)
        {
            var provider = services.BuildServiceProvider();
            var configurationFactory = provider.GetService<IDbConfigurationFactory>();
            configurationFactory.Register(options, new SqlProvider(options));
            return services;
        }
    }
}
