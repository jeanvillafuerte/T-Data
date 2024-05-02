using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;

namespace Thomas.Database.Cache
{
    internal abstract class CacheTypeParser
    {
        internal static HashSet<Type> CachedTypes = new HashSet<Type>(10);
    }

    internal sealed class CacheTypeParser<T> : CacheTypeParser where T : class, new()
    {
        internal static ConcurrentDictionary<ulong, Func<DbDataReader, T>> TypeParserDictionary = new ConcurrentDictionary<ulong, Func<DbDataReader, T>>(Environment.ProcessorCount * 2, 10);

        private CacheTypeParser() { }

        internal static void Set(in ulong key, in Func<DbDataReader, T> value)
        {
            TypeParserDictionary.TryAdd(key, value);
            CachedTypes.Add(typeof(T));
        }

        internal static bool TryGet(in ulong key, out Func<DbDataReader, T> properties) => TypeParserDictionary.TryGetValue(key, out properties);
        internal static void Clear()
        {
            TypeParserDictionary.Clear();
            TypeParserDictionary = new ConcurrentDictionary<ulong, Func<DbDataReader, T>>(Environment.ProcessorCount * 2, 10);
        }
    }
}
