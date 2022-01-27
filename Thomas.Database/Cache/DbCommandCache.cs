using System.Collections.Concurrent;
using System.Data.Common;

namespace Thomas.Database.Cache
{
    internal sealed class DbCommandCache
    {
        private static DbCommandCache instance;

        internal static DbCommandCache Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DbCommandCache();
                }
                return instance;
            }
        }

        private static readonly ConcurrentDictionary<string, DbCommand> Data = new ConcurrentDictionary<string, DbCommand>();

        internal void Set(string key, DbCommand value)
        {
            Data[key] = value;
        }

        internal bool TryGet(string key, out DbCommand types)
        {
            return Data.TryGetValue(key, out types);
        }
    }
}
