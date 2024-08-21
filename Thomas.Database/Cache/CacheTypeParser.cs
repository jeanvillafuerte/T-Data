using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;

namespace Thomas.Database.Cache
{
    internal abstract class CacheTypeHash
    {
        internal static HashSet<Type> CachedTypes = new HashSet<Type>();
    }

    internal sealed class CacheTypeParser<T> : CacheTypeHash
    {
        internal static ConcurrentDictionary<int, Func<DbDataReader, T>> TypeParserDictionary = new ConcurrentDictionary<int, Func<DbDataReader, T>>(Environment.ProcessorCount * 2, 10);
        
        private CacheTypeParser() { }

        internal static void Set(in int key, in Func<DbDataReader, T> value)
        {
            TypeParserDictionary.TryAdd(key, value);

            lock (CachedTypes)
            {
              CachedTypes.Add(typeof(T));
            }
        }

        internal static bool TryGet(in int key, out Func<DbDataReader, T> properties) => TypeParserDictionary.TryGetValue(key, out properties);
        internal static void Clear()
        {
            TypeParserDictionary.Clear();
            TypeParserDictionary = new ConcurrentDictionary<int, Func<DbDataReader, T>>(Environment.ProcessorCount * 2, 10);
        }
    }
}
