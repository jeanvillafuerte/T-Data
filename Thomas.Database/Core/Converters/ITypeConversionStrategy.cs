using System;
using System.Globalization;

namespace Thomas.Database.Core.Converters
{
    internal interface ITypeConversionStrategy
    {
        object Convert(object value, CultureInfo cultureInfo);
        bool CanConvert(Type targetType, object value);
        bool CanConvertByType(object value);
        object ConvertByType(object value, CultureInfo cultureInfo);
    }
}
