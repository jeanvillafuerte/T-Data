using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Thomas.Database.Cache.Metadata
{
    public sealed class MetadataCacheManager
    {
        private static MetadataCacheManager instance;
        private static readonly ConcurrentDictionary<string, Dictionary<string, MetadataPropertyInfo>> Data = new ConcurrentDictionary<string, Dictionary<string, MetadataPropertyInfo>>();

        public static MetadataCacheManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MetadataCacheManager();
                }
                return instance;
            }
        }

        private MetadataCacheManager() { }

        public void Set(string key, Dictionary<string, MetadataPropertyInfo> value) => Data.TryAdd(key, value);

        public bool TryGet(string key, out Dictionary<string, MetadataPropertyInfo> meta) => Data.TryGetValue(key, out meta!);
    }
}
