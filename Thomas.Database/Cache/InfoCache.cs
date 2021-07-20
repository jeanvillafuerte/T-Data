using System.Collections.Generic;

namespace Thomas.Database.Cache
{
    internal sealed class InfoCache
    {
        public InfoCache( Dictionary<string, InfoProperty> infoProperties)
        {
            InfoProperties = infoProperties;
        }

        internal Dictionary<string, InfoProperty> InfoProperties { get; set; }
    }
}
