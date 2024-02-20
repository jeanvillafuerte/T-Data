using System;
using System.Collections.Concurrent;

namespace Thomas.Database.Cache
{
    public sealed class CacheDbParameterArray
    {
        private static readonly ConcurrentDictionary<string, MetadataParameters[]> DataParameters = new ConcurrentDictionary<string, MetadataParameters[]>(Environment.ProcessorCount * 2, 100);

        private static CacheDbParameterArray instance;
        public static CacheDbParameterArray Instance
        {
            get
            {
                instance ??= new CacheDbParameterArray();
                return instance;
            }
        }

        internal static void Set(in string key, in MetadataParameters[] values) => DataParameters.TryAdd(key, values);
        internal static bool TryGet(in string key, ref MetadataParameters[] values)
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
