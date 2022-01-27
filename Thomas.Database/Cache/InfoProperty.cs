using System;
using System.Reflection;

namespace Thomas.Database.Cache
{
    public sealed class InfoProperty
    {
        public InfoProperty(PropertyInfo info, Type? type)
        {
            Info = info;
            Type = type;
        }

        public PropertyInfo Info { get; set; }
        public Type? Type { get; set; }
    }
}