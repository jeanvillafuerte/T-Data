using System;
using TData.Cache.MemoryCache;
using TData.Cache.MemoryCache.Sqlite;
using TData.Configuration;

namespace TData.Cache
{
    public sealed class DbCacheConfig
    {
        public static void Register(in DbSettings dbSettings, in CacheSettings cacheSettings)
        {
            if (cacheSettings.Provider == DbCacheProvider.Sqlite)
            {
                SqliteDataCache.Initialize(in dbSettings.Signature, in cacheSettings);
            }

            CachedDbHub.CacheDbDictionary.TryAdd(dbSettings.Signature, new DbDataCache(cacheSettings.TTL));
            DbConfig.Register(in dbSettings);
        }
    }

    public delegate object SerializeDelegate(in object data);
    public delegate object DeSerializeDelegate(in object rawData, in Type type, in bool treatAsList);

    public sealed class CacheSettings
    {
        public DbCacheProvider Provider { get; }
        public TimeSpan TTL { get; set; }
        public readonly bool IsTextFormat;
        public readonly SerializeDelegate SerializeDelegate;
        public readonly DeSerializeDelegate DeserializeDelegate;

        public CacheSettings(in DbCacheProvider provider)
        {
            Provider = provider;
        }

        public CacheSettings(in DbCacheProvider provider, in bool isTextFormat, in SerializeDelegate serializer, in DeSerializeDelegate deserializer)
        {
            Provider = provider;
            IsTextFormat = isTextFormat;
            SerializeDelegate = serializer;
            DeserializeDelegate = deserializer;
        }
    }
}
