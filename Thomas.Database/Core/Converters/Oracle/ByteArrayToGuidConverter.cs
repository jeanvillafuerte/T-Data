using System;
using System.Globalization;

namespace Thomas.Database.Core.Converters.Oracle
{
    public class ByteArrayToGuidConverter : ITypeConversionStrategy
    {
        public object Convert(object value, CultureInfo cultureInfo)
        {
            return new Guid(value as byte[]);
        }

        public bool CanConvert(Type targetType, object value)
        {
            return targetType == typeof(Guid) && value is byte[] byteArray && byteArray.Length == 16;
        }

        public bool CanConvertByType(object value)
        {
            return value is Guid;
        }

        public object ConvertByType(object value, CultureInfo cultureInfo)
        {
            return ((Guid)value).ToByteArray();
        }
    }
}
