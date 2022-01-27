using System;
using Microsoft.Extensions.DependencyInjection;

namespace Thomas.Database.SqlServer
{
    using Strategy;

    public static class IServiceCollectionExtension
    {
        public static IServiceCollection AddThomasSqlDatabase(this IServiceCollection services, Func<IServiceProvider, ThomasDbStrategyOptions> options)
        {
            services.AddScoped(options);
            services.AddScoped<IDatabaseProvider, SqlProvider>();
            services.AddScoped<IThomasDb, ThomasDb>();

            var serviceProvider = services.BuildServiceProvider();

            (int processors, string culture) = GetMaxDegreeOfParallelism(options, serviceProvider);

            if (processors == 1)
            {
                services.AddScoped<JobStrategy>(_ => new SimpleJobStrategy(culture, 1));
            }
            else
            {
                services.AddScoped<JobStrategy>(_ => new MultithreadJobStrategy(culture, processors));
            }

            return services;
        }

        private static (int, string) GetMaxDegreeOfParallelism(Func<IServiceProvider, ThomasDbStrategyOptions> opt, IServiceProvider provider)
        {
            var options = opt.Invoke(provider);

            string culture = options.Culture;

            if (options.MaxDegreeOfParallelism == 1 || options.MaxDegreeOfParallelism == 0)
            {
                return (1, culture);
            }
            else
            {
                return (Environment.ProcessorCount >= options.MaxDegreeOfParallelism ? options.MaxDegreeOfParallelism : 1 , culture);
            }
        }

    }
}
