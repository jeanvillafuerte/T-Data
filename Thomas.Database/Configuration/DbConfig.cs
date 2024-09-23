using System;
using System.Collections.Concurrent;
using System.Linq;
using Thomas.Database.Core.FluentApi;
using Thomas.Database.Core.Provider;

namespace Thomas.Database.Configuration
{
    public sealed class DbConfig
    {
        private static readonly ConcurrentDictionary<int, DbSettings> dictionary = new ConcurrentDictionary<int, DbSettings>(Environment.ProcessorCount * 2, 10);
        internal static ConcurrentDictionary<string, DbTable> Tables = new ConcurrentDictionary<string, DbTable>(Environment.ProcessorCount * 2, 10);

        /// <summary>
        /// Clears all the configurations and tables.
        /// </summary>
        public static void Clear()
        {
            dictionary.Clear();
            Tables.Clear();
        }

        /// <summary>
        /// Registers a new database configuration.
        /// </summary>
        /// <param name="config">The database settings to register.</param>
        /// <exception cref="DuplicateSignatureException">Thrown when a configuration with the same signature already exists.</exception>
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

        internal static DbSettings Get()
        {
            if (dictionary.Count == 1)
                return dictionary.Values.First();
            else
                throw new Exception("There is more than one configuration, use the method Get(string signature)");
        }

        internal static DbSettings Get(in string signature)
        {
            var key = HashHelper.GenerateHash(signature);
            dictionary.TryGetValue(key, out var configuration);

            return configuration;
        }

        internal static void AddTableBuilder(in TableBuilder tableBuilder)
        {
            Tables = new ConcurrentDictionary<string, DbTable>(tableBuilder.Tables);
        }
    }
}
