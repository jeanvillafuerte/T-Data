using System.Collections.Concurrent;

namespace Thomas.Database.Cache
{
    internal sealed class MetadataCache
    {
        private static MetadataCache instance;

        private static readonly ConcurrentDictionary<string, MetadataProperties> Data = new ConcurrentDictionary<string, MetadataProperties>();

        internal static MetadataCache Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MetadataCache();
                }
                return instance;
            }
        }

        private MetadataCache()
        {

        }

        internal void Set(string key, MetadataProperties value)
        {
            Data[key] = value;
        }

        internal bool TryGet(string key, out MetadataProperties types)
        {
            return Data.TryGetValue(key, out types);
        }

    }
}
