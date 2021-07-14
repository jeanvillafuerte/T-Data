using System;
using Microsoft.Extensions.DependencyInjection;

namespace Thomas.Database.SqlServer
{
    public static class IServiceCollectionExtension
    {
        public static IServiceCollection AddThomasSqlDatabase(this IServiceCollection services, Func<IServiceProvider, ThomasDbStrategyOptions> options)
        {
            services.AddScoped(options);
            services.AddScoped<IDatabaseProvider, SqlProvider>();
            services.AddScoped<IThomasDb, ThomasDb>();
            return services;
        }
    }
}
