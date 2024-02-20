using System;
using System.Globalization;

namespace Thomas.Database.Core.Converters.Oracle
{
    public class GuidToByteArrayConverter : ITypeConversionStrategy
    {
        public bool CanConvert(Type targetType, object value)
        {
            return targetType == typeof(byte[]) && value is Guid;
        }

        public bool CanConvertByType(object value)
        {
            return false;
        }

        public object Convert(object value, CultureInfo cultureInfo)
        {
            return ((Guid)value).ToByteArray();
        }

        public object ConvertByType(object value, CultureInfo cultureInfo)
        {
            return null;
        }
    }
}
