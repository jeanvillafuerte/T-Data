using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;


namespace Thomas.Database.Cache
{
    internal sealed class CacheThomas
    {
        private static CacheThomas instance;

        private static ConcurrentDictionary<string, (IDictionary<string, PropertyInfo>, bool?)> Data = new ConcurrentDictionary<string, (IDictionary<string, PropertyInfo>, bool?)>();

        public static CacheThomas Instance
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

        public void Set(string key, (IDictionary<string, PropertyInfo>, bool?) value)
        {
            Data[key] = value;
        }

        public bool TryGet(string key, out (IDictionary<string, PropertyInfo>, bool?) types)
        {
            return Data.TryGetValue(key, out types);
        }
    }
}
