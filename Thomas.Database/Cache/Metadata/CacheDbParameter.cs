using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Thomas.Database.SqlServer")]

namespace Thomas.Database.Cache.Metadata
{
    public sealed class CacheDbParameter<TDbType> where TDbType : Enum
    {
        private static readonly ConcurrentDictionary<string, MetadataParameters<TDbType>[]> DataParameters = new ConcurrentDictionary<string, MetadataParameters<TDbType>[]>(Environment.ProcessorCount * 2, 100);

        private static CacheDbParameter<TDbType> instance;
        public static CacheDbParameter<TDbType> Instance
        {
            get
            {
                instance ??= new CacheDbParameter<TDbType>();
                return instance;
            }
        }

        internal static void Set(in string key, in MetadataParameters<TDbType>[] values) => DataParameters.TryAdd(key, values);
        internal static bool TryGet(in string key, ref MetadataParameters<TDbType>[] values)
        {
            var result = DataParameters.TryGetValue(key, out var parameters);

            if (!result)
            {
                return false;
            }

            values = parameters;
            return true;
        }

        public void Clear() => DataParameters.Clear();
    }
}
