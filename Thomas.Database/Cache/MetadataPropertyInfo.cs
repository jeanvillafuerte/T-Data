using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Thomas.Database.Core.Converters;

namespace Thomas.Database.Cache
{
    internal struct MetadataPropertyInfo
    {
        internal MetadataPropertyInfo(in PropertyInfo info)
        {
            PropertyInfo = info;

            if (PropertyInfo.PropertyType.IsGenericType)
                Type = Nullable.GetUnderlyingType(info.PropertyType);
            else
                Type = info.PropertyType;
        }

        internal string Name => PropertyInfo.Name;
        internal PropertyInfo PropertyInfo { get; }
        internal Type? Type { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetValue<Titem, TValue>(in Titem item, in TValue value, in CultureInfo cultureInfo, in ITypeConversionStrategy[] converters) where Titem : class
        {
            var convertedValue = TypeConversionRegistry.Convert(value, Type!, in cultureInfo, in converters);
            var setter = PropertyInfo.SetMethod.CreateDelegate(typeof(Action<,>).MakeGenericType(PropertyInfo.DeclaringType, PropertyInfo.PropertyType));
            setter.DynamicInvoke(item, convertedValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetValue<T>(in T item)
        {
            var getter = PropertyInfo.GetMethod!.CreateDelegate(typeof(Func<,>).MakeGenericType(PropertyInfo.DeclaringType!, PropertyInfo.PropertyType));
            return getter.DynamicInvoke(item) ?? DBNull.Value;
        }

        public string ErrorFormat(in object value)
        {
            var val = GetValue(in value);
            return $"\t" + PropertyInfo.Name + " : " + (val is null ? "NULL" : val) + " ";
        }
    }
}