using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Thomas.Cache
{
    public static class IServiceCollectionExtension
    {
        public static IServiceCollection AddDbResultCached(this IServiceCollection services)
        {
            services.TryAddSingleton<ICachedDatabase, CachedDatabase>();
            return services;
        }
    }
}
