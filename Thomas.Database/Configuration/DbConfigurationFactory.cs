using System;
using System.Collections.Concurrent;
using Thomas.Database.Core.FluentApi;
using Thomas.Database.Exceptions;

namespace Thomas.Database.Configuration
{
    public sealed class DbConfigurationFactory
    {
        private static readonly ConcurrentDictionary<string, DbSettings> dictionary = new ConcurrentDictionary<string, DbSettings>(Environment.ProcessorCount * 2, 10);
        internal static ConcurrentDictionary<string, DbTable> Tables = new ConcurrentDictionary<string, DbTable>(Environment.ProcessorCount * 2, 10);

        public static void Register(in DbSettings config)
        {
            if (!dictionary.TryAdd(config.Signature, config))
                throw new DuplicateSignatureException();
        }

        public static DbSettings Get(in string signature)
        {
            dictionary.TryGetValue(signature, out var configuration);

            return configuration;
        }

        public static void AddTableBuilder(TableBuilder tableBuilder)
        {
            Tables = new ConcurrentDictionary<string, DbTable>(tableBuilder.Tables);
        }
    }
}
