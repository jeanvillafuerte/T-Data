using System;
using System.Collections.Concurrent;
using Thomas.Database.Exceptions;

namespace Thomas.Database.Configuration
{
    public sealed class DbConfigurationFactory
    {
        private static readonly ConcurrentDictionary<string, DbSettings> dictionary = new ConcurrentDictionary<string, DbSettings>(Environment.ProcessorCount * 2, 10);
        private static readonly ConcurrentDictionary<string, IDatabaseProvider> dictionaryProvider = new ConcurrentDictionary<string, IDatabaseProvider>(Environment.ProcessorCount * 2, 10);


        public static void Register(in DbSettings config, in IDatabaseProvider provider)
        {
            if (!dictionary.TryAdd(config.Signature, config))
                throw new DuplicateSignatureException();

            if (!dictionaryProvider.TryAdd(config.Signature, provider))
                throw new DuplicateSignatureException();
        }

        public static (DbSettings, IDatabaseProvider) Get(in string signature)
        {
            dictionary.TryGetValue(signature, out var configuration);
            dictionaryProvider.TryGetValue(signature, out var provider);

            return (configuration, provider);
        }
    }
}
