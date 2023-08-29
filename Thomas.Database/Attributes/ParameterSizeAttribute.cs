using System;

namespace Thomas.Database.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ParameterSizeAttribute : Attribute
    {
        public int Size { get; set; }

        public ParameterSizeAttribute(int size)
        {
            Size = size;
        }
    }
}
