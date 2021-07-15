using System.Collections.Concurrent;

namespace Thomas.Database.Cache
{
    internal sealed class CacheThomas
    {
        private static CacheThomas instance;

        private static ConcurrentDictionary<string, InfoCache> Data = new ConcurrentDictionary<string, InfoCache>();

        internal static CacheThomas Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CacheThomas();
                }
                return instance;
            }
        }

        private CacheThomas()
        {

        }

        internal void Set(string key, InfoCache value)
        {
            Data[key] = value;
        }

        internal bool TryGet(string key, out InfoCache types)
        {
            return Data.TryGetValue(key, out types);
        }
    }
}
