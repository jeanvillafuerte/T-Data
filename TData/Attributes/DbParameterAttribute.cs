using System;
using System.Data;

namespace TData.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DbParameterAttribute: Attribute
    {
        public byte Size { get; set; }
        public byte Precision { get; set; }
        public byte Scale { get; set; }
        public string Name { get; set; }
        public byte Order { get; set; }
        public ParameterDirection Direction { get; set; }
        public bool IsOracleCursor { get; set; }

        public bool IsOutput => Direction == ParameterDirection.Output || Direction == ParameterDirection.InputOutput || Direction == ParameterDirection.ReturnValue;

        public DbParameterAttribute(string name = null, byte size = 0, byte precision = 0, byte scale = 0, ParameterDirection direction = ParameterDirection.Input, byte order = 0, bool isOracleCursor = false)
        {
            Name = name;
            Size = size;
            Precision = precision;
            Scale = scale;
            Order = order;
            Direction = direction;
            IsOracleCursor = isOracleCursor;
        }
    }
}
