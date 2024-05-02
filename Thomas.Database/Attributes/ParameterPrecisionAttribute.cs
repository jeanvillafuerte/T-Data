using System;

namespace Thomas.Database.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ParameterPrecisionAttribute : Attribute
    {
        public int Precision { get; set; }

        public ParameterPrecisionAttribute(int precision)
        {
            Precision = precision;
        }
    }
}
