using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Oracle = Thomas.Database.Core.Converters.Oracle;
using SQLite = Thomas.Database.Core.Converters.SQLite;

namespace Thomas.Database.Core.Converters
{
    internal static class TypeConversionRegistry
    {
        internal readonly static ConcurrentDictionary<SqlProvider, List<IInParameterValueConverter>> InParameterValueConverters = new ConcurrentDictionary<SqlProvider, List<IInParameterValueConverter>>();
        internal readonly static ConcurrentDictionary<SqlProvider, List<IOutParameterValueConverter>> OutParameterValueConverters = new ConcurrentDictionary<SqlProvider, List<IOutParameterValueConverter>>();

        static TypeConversionRegistry()
        {
            InParameterValueConverters[SqlProvider.Oracle] = new List<IInParameterValueConverter> { new Oracle.GuidToByteArrayConverter() };
            InParameterValueConverters[SqlProvider.Sqlite] = new List<IInParameterValueConverter> { new SQLite.GuidConverter(), new SQLite.TimeSpanConverter() };
            InParameterValueConverters[SqlProvider.SqlServer] = Enumerable.Empty<IInParameterValueConverter>().ToList();
            InParameterValueConverters[SqlProvider.MySql] = Enumerable.Empty<IInParameterValueConverter>().ToList();
            InParameterValueConverters[SqlProvider.PostgreSql] = Enumerable.Empty<IInParameterValueConverter>().ToList();

            OutParameterValueConverters[SqlProvider.Oracle] = new List<IOutParameterValueConverter> { new Oracle.GuidToByteArrayConverter() };
            OutParameterValueConverters[SqlProvider.Sqlite] = new List<IOutParameterValueConverter> { new SQLite.GuidConverter(), new SQLite.TimeSpanConverter() };
            OutParameterValueConverters[SqlProvider.SqlServer] = Enumerable.Empty<IOutParameterValueConverter>().ToList();
            OutParameterValueConverters[SqlProvider.MySql] = Enumerable.Empty<IOutParameterValueConverter>().ToList();
            OutParameterValueConverters[SqlProvider.PostgreSql] = Enumerable.Empty<IOutParameterValueConverter>().ToList();
        }

        public static void RegisterInParameterConverter(SqlProvider provider, IInParameterValueConverter converter)
        {
            InParameterValueConverters[provider].Add(converter);
        }

        public static void RegisterOutParameterConverter(SqlProvider provider, IOutParameterValueConverter converter)
        {
            OutParameterValueConverters[provider].Add(converter);
        }

        public static object ConvertInParameterValue(in SqlProvider provider, object value, Type sourceType, in bool reduceNumericToIntegerWhenPossible = true)
        {
            var converters = InParameterValueConverters[provider].Where(x => x.CanConvert(value)).ToList();

           if (converters.Count > 1)
                throw new InvalidOperationException($"Multiple in-parameter value converters found for type {sourceType.Name} in provider {provider}");

            if (converters.Count == 1)
                return converters[0].ConvertInValue(value);

            //check if value is a convertible numeric type
            if (reduceNumericToIntegerWhenPossible &&
               (value is decimal || value is double || value is float)
               && int.TryParse(value.ToString(), out var intValue))
            {
                return intValue;
            }

            // Fall-back for unregistered type/provider combinations
            return Convert.ChangeType(value, sourceType, CultureInfo.InvariantCulture);
        }

        public static object ConvertOutParameterValue(in SqlProvider provider, object value, Type targetType, in bool reduceNumericToIntegerWhenPossible = true)
        {
            var converters = OutParameterValueConverters[provider].Where(x => x.CanConvert(value, targetType)).ToList();

            if (converters.Count > 1)
                throw new InvalidOperationException($"Multiple out-parameter value converters found for type {targetType.Name} in provider {provider}");

            if (converters.Count == 1)
                return converters[0].ConvertOutValue(value);

            //check if value is a convertible numeric type and target type is integer
            if (reduceNumericToIntegerWhenPossible &&
               (value is decimal || value is double || value is float)
               && targetType == typeof(int)
               && int.TryParse(value.ToString(), out var intValue))
            {
                return intValue;
            }

            //in case of null value, return default value of target type
            if (DBNull.Value.Equals(value))
            {
                var underlyingType = Nullable.GetUnderlyingType(targetType);

                if (underlyingType != null || !targetType.IsValueType)
                    return null;

                throw new DbNullToValueTypeException($"Cannot assign null to value type '{targetType.Name}' as an output parameter. Ensure that the database field is not null or use a nullable type for this parameter.");
            }

            //string to guid
            if (value is string && targetType == typeof(Guid) && Guid.TryParse((string)value, out var guid))
                return guid;

            //string to TimeSpan
            if (value is string && targetType == typeof(TimeSpan))
                return CommonConversion.SafeConversionStringToTimeSpan((string)value);

            // Fall-back for unregistered type/provider combinations
            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        public static bool TryGetInParameterConverter(in SqlProvider provider, Type propertyType, out IInParameterValueConverter converter)
        {
            var converters = InParameterValueConverters[provider].Where(x => x.SourceType == propertyType).ToList();

            if (converters.Count > 1)
                throw new InvalidOperationException($"Multiple in-parameter value converters found for type {propertyType.Name} in provider {provider}");

            if (converters.Count == 1)
            {
                converter = converters[0];
                return true;
            }

            converter = null;
            return false;
        }
    }
}
