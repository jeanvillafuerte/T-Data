using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Thomas.Cache")]
namespace Thomas.Database.Cache
{
    internal sealed class DynamicQueryString
    {
        private static ConcurrentDictionary<int, ValueTuple<string, Type>> DynamicQueryStringDictionary = new ConcurrentDictionary<int, ValueTuple<string, Type>>(Environment.ProcessorCount * 2, 50);

        private DynamicQueryString() { }

        internal static void Set(in int key, ValueTuple<string, Type> value) => DynamicQueryStringDictionary.TryAdd(key, value);
        internal static bool TryGet(in int key, out ValueTuple<string, Type> meta) => DynamicQueryStringDictionary.TryGetValue(key, out meta);
        public static void Clear()
        {
            DynamicQueryStringDictionary.Clear();
            DynamicQueryStringDictionary = new ConcurrentDictionary<int, ValueTuple<string, Type>>(Environment.ProcessorCount * 2, 50);
        }
    }
}
