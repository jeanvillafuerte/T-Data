using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Thomas.Database.Core.Converters.Oracle;

namespace Thomas.Database.Core.Converters
{
    internal class TypeConversionRegistry
    {
        internal readonly static Dictionary<SqlProvider, ITypeConversionStrategy[]> Strategies = new Dictionary<SqlProvider, ITypeConversionStrategy[]>();

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
            Strategies[provider] = converters;
        }

        public static object Convert(object value, Type targetType, in CultureInfo cultureInfo, in ITypeConversionStrategy[] converters)
        {
            var converter = converters.Where(x => x.CanConvert(targetType, value)).FirstOrDefault();
            if (converter != null)
                return converter.Convert(value, cultureInfo);

            // Fallback for unregistered type/provider combinations
            return System.Convert.ChangeType(value, targetType, cultureInfo);
        }

        public static object ConvertByType(object value, Type targetType, CultureInfo cultureInfo, ITypeConversionStrategy[] converters)
        {
            var converter = converters.Where(x => x.CanConvertByType(value)).FirstOrDefault();
            if (converter != null)
                return converter.ConvertByType(value, cultureInfo);

            return System.Convert.ChangeType(value, targetType, cultureInfo);
        }
    }

    internal class DbDataConverter
    {
        internal ITypeConversionStrategy[] Converters;
        public DbDataConverter(SqlProvider provider) => Converters = TypeConversionRegistry.Strategies[provider];
    }
}
