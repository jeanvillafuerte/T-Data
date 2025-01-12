using System;
using System.Collections.Concurrent;
using System.Linq;
using TData.Core.FluentApi;
using TData.Core.Provider;

namespace TData.Configuration
{
    public sealed class DbConfig
    {
        private static readonly ConcurrentDictionary<int, DbSettings> dictionary = new ConcurrentDictionary<int, DbSettings>(Environment.ProcessorCount * 2, 10);
        internal static ConcurrentDictionary<string, DbTable> Tables = new ConcurrentDictionary<string, DbTable>(Environment.ProcessorCount * 2, 10);
        private static int _defaultSignatureHash;
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
        /// <param name="settings">The database settings to register.</param>
        /// <exception cref="DuplicateSignatureException">Thrown when a configuration with the same signature already exists.</exception>
        public static void Register(in DbSettings settings)
        {
            var key = HashHelper.GenerateHash(settings.Signature);

            if (settings.DefaultDb)
            {
                if (_defaultSignatureHash == 0)
                    _defaultSignatureHash = key;
                else
                    throw new Exception("There is already a default configuration");
            }

            ValidateConfiguration(in settings);
            if (!dictionary.TryAdd(key, settings))
                throw new DuplicateSignatureException();

            DatabaseHelperProvider.LoadConnectionDelegate(settings.SqlProvider);
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
            if (dictionary.TryGetValue(_defaultSignatureHash, out var db))
            {
                return db;
            }
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
            if (Tables.Any())
            {
                foreach (var table in tableBuilder.Tables)
                {
                    Tables.TryAdd(table.Key, table.Value);
                }
            }
            else
            {
                Tables = new ConcurrentDictionary<string, DbTable>(tableBuilder.Tables);
            }
        }
    }
}
