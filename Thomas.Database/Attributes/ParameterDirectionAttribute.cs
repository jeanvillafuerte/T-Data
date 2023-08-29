using System;

namespace Thomas.Database.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ParameterDirectionAttribute : Attribute
    {
        public ParamDirection Direction { get; set; }

        public ParameterDirectionAttribute(ParamDirection direction)
        {
            Direction = direction;
        }
    }
}
