using System;
using System.Reflection;

namespace Thomas.Database.Cache
{
    internal sealed class InfoProperty
    {
        internal InfoProperty(PropertyInfo info, Type? type)
        {
            Info = info;
            Type = type;
        }

        internal PropertyInfo Info { get; set; }
        internal Type? Type { get; set; }
    }
}
