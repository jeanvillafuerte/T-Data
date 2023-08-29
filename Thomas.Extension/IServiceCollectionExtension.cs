using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Thomas.Cache.Factory;
using Thomas.Cache.Manager;

namespace Thomas.Cache
{
    public static class IServiceCollectionExtension
    {
        public static IServiceCollection AddDbResultCached(this IServiceCollection services)
        {
            services.TryAddSingleton<ICachedDatabase, CachedDatabase>();
            services.TryAddSingleton<IDbResultCachedFactory, DbResultCachedFactory>();
            return services;
        }
    }
}
