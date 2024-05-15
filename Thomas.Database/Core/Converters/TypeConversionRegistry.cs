using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using Thomas.Database.Core.Converters.Oracle;

namespace Thomas.Database.Core.Converters
{
    internal static class TypeConversionRegistry
    {
        internal readonly static ConcurrentDictionary<SqlProvider, ITypeConversionStrategy[]> ProviderConverters = new ConcurrentDictionary<SqlProvider, ITypeConversionStrategy[]>(Environment.ProcessorCount * 2, 5);

        static TypeConversionRegistry()
        {
            RegisterStrategy(SqlProvider.Oracle, new ITypeConversionStrategy[] { new ByteArrayToGuidConverter(), new GuidToByteArrayConverter() });
            RegisterStrategy(SqlProvider.Sqlite, new ITypeConversionStrategy[] { });
            RegisterStrategy(SqlProvider.SqlServer, new ITypeConversionStrategy[] { });
            RegisterStrategy(SqlProvider.MySql, new ITypeConversionStrategy[] { });
            RegisterStrategy(SqlProvider.PostgreSql, new ITypeConversionStrategy[] { });
        }

        private static void RegisterStrategy(SqlProvider provider, ITypeConversionStrategy[] converters)
        {
            ProviderConverters[provider] = converters;
        }

        public static object Convert(object value, Type targetType, in CultureInfo cultureInfo, SqlProvider provider)
        {
            var converter = ProviderConverters[provider].FirstOrDefault(x => x.CanConvert(targetType, value));
            if (converter != null)
                return converter.Convert(value, cultureInfo);

            // Fall-back for unregistered type/provider combinations
            return System.Convert.ChangeType(value, targetType, cultureInfo);
        }

        public static object ConvertByType(object value, Type targetType, in SqlProvider provider, bool reduceNumericToIntegerWhenPossible = false)
        {
            var converter = ProviderConverters[provider].FirstOrDefault(x => x.CanConvertByType(value));
            if (converter != null)
                return converter.ConvertByType(value, CultureInfo.InvariantCulture);

            if (reduceNumericToIntegerWhenPossible &&
                (value is decimal || value is double || value is float)
                && int.TryParse(value.ToString(), out var intValue))
            {
                return intValue;
            }

            return System.Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
    }
}
