using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Thomas.Database.Configuration;
using Thomas.Database.Strategy.Factory;

namespace Thomas.Database
{
    public static class IServiceCollectionExtension
    {
        public static IServiceCollection AddDbFactory(this IServiceCollection services)
        {
            services.TryAddSingleton<IDbConfigurationFactory, DbConfigurationFactory>();
            services.TryAddSingleton<IDbFactory, DbFactory>();
            return services;
        }
    }
}
