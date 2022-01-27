using System.Collections.Generic;

namespace Thomas.Database.Cache
{
    internal sealed class MetadataProperties
    {
        public MetadataProperties(Dictionary<string, InfoProperty> infoProperties)
        {
            InfoProperties = infoProperties;
        }

        internal Dictionary<string, InfoProperty> InfoProperties { get; set; }
    }

}
