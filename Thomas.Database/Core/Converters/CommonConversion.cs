using System;
using System.Globalization;

namespace Thomas.Database.Core.Converters
{
    internal static class CommonConversion
    {
        internal static TimeSpan SafeConversionStringToTimeSpan(string value)
        {
            if (TimeSpan.TryParseExact(value, new[] { "hh\\:mm\\:ss", "'hh':'mm':'ss'.'FFFFFFF", "d' 'hh':'mm':'ss'.'FFFFFFF" }, CultureInfo.InvariantCulture, out var outValue))
                return outValue;

            throw new TimeSpanConversionException($"Cannot convert string value {value} to TimeSpan");
        }
    }
}
