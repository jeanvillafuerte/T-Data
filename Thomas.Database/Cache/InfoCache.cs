using System.Collections.Generic;

namespace Thomas.Database.Cache
{
    internal sealed class InfoCache
    {
        public InfoCache(bool containNullables, IDictionary<string, InfoProperty> infoProperties)
        {
            ContainNullables = containNullables;
            InfoProperties = infoProperties;
        }

        internal bool ContainNullables { get; set; }
        internal IDictionary<string, InfoProperty> InfoProperties { get; set; }
    }
}
