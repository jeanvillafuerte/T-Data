using System.Collections.Concurrent;
using Thomas.Database.Exceptions;

namespace Thomas.Database.Configuration
{
    public interface IDbConfigurationFactory
    {
        void Register(ThomasDbStrategyOptions configuration, IDatabaseProvider provider);
        (ThomasDbStrategyOptions, IDatabaseProvider) Get(string signature);
    }

    public sealed class DbConfigurationFactory : IDbConfigurationFactory
    {
        private static readonly ConcurrentDictionary<string, ThomasDbStrategyOptions> dictionary = new ConcurrentDictionary<string, ThomasDbStrategyOptions>();
        private static readonly ConcurrentDictionary<string, IDatabaseProvider> dictionaryProvider = new ConcurrentDictionary<string, IDatabaseProvider>();

        public void Register(ThomasDbStrategyOptions configuration, IDatabaseProvider provider)
        {
            dictionary.TryAdd(configuration.Signature, configuration);
            dictionaryProvider.TryAdd(configuration.Signature, provider);
        }

        public (ThomasDbStrategyOptions, IDatabaseProvider) Get(string signature)
        {
            dictionary.TryGetValue(signature, out var configuration);
            dictionaryProvider.TryGetValue(signature, out var provider);

            if (configuration == null)
                throw new DbSignatureNotFoundException($"Configuration for \"{signature}\" cannot found");

            if (provider == null)
                throw new DbProviderNotFoundException($"Provider for \"{signature}\" cannot found");

            return (configuration, provider);
        }
    }
}
