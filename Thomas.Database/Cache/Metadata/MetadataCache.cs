using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Thomas.Database.Cache.Metadata
{
    public sealed class MetadataCacheManager
    {
        private static MetadataCacheManager instance;
        private static readonly ConcurrentDictionary<string, Dictionary<string, MetadataPropertyInfo>> ResponseTypes = new ConcurrentDictionary<string, Dictionary<string, MetadataPropertyInfo>>();
        private static readonly ConcurrentDictionary<string, MetadataPropertyInfo[]> DataParameters = new ConcurrentDictionary<string, MetadataPropertyInfo[]>();

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

        public void Set(string key, Dictionary<string, MetadataPropertyInfo> value) => ResponseTypes.TryAdd(key, value);
        public bool TryGet(string key, out Dictionary<string, MetadataPropertyInfo> meta) => ResponseTypes.TryGetValue(key, out meta!);

        public void Set(string key, MetadataPropertyInfo[] values) => DataParameters.TryAdd(key, values);
        public bool TryGet(string key, out MetadataPropertyInfo[] values) => DataParameters.TryGetValue(key, out values!);
    }
}
