using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;

namespace Thomas.Database.SqlServer.Cache
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

        private static readonly ConcurrentDictionary<string, SqlCommand> Data = new ConcurrentDictionary<string, SqlCommand>();

        internal void Set(string key, SqlCommand value)
        {
            Data[key] = value;
        }

        internal bool TryGet(string key, out SqlCommand types)
        {
            return Data.TryGetValue(key, out types);
        }
    }
}
