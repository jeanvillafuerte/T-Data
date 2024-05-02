using System;
using System.Collections.Concurrent;
using Thomas.Database.Core.FluentApi;
using Thomas.Database.Core.Provider;
using Thomas.Database.Exceptions;

namespace Thomas.Database.Configuration
{
    public sealed class DbConfigurationFactory
    {
        private static readonly ConcurrentDictionary<ulong, DbSettings> dictionary = new ConcurrentDictionary<ulong, DbSettings>(Environment.ProcessorCount * 2, 10);
        internal static ConcurrentDictionary<string, DbTable> Tables = new ConcurrentDictionary<string, DbTable>(Environment.ProcessorCount * 2, 10);

        public static void Register(in DbSettings config)
        {
            var key = HashHelper.GenerateHash(config.Signature);
            if (!dictionary.TryAdd(key, config))
                throw new DuplicateSignatureException();

            ValidateConfiguration(in config);
            DatabaseHelperProvider.LoadConnectionDelegate(config.SqlProvider);
        }

        static void ValidateConfiguration(in DbSettings config)
        {
            if (string.IsNullOrEmpty(config.Signature))
                throw new ArgumentException("Signature cannot be null or empty");
            if (string.IsNullOrEmpty(config.StringConnection))
                throw new ArgumentException($"String connection cannot be null or empty, Signature ({config.Signature})");
            if (config.ConnectionTimeout < 0)
                throw new ArgumentException($"Connection timeout cannot be less than 0, Signature ({config.Signature})");
        }

        public static DbSettings Get(in string signature)
        {
            var key = HashHelper.GenerateHash(signature);
            dictionary.TryGetValue(key, out var configuration);

            return configuration;
        }

        public static void AddTableBuilder(TableBuilder tableBuilder)
        {
            Tables = new ConcurrentDictionary<string, DbTable>(tableBuilder.Tables);
        }

        public static void AddTable(DbTable table)
        {
            Tables.TryAdd(table.Name, table);
        }
    }
}
