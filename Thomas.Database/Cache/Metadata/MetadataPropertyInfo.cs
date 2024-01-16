using System;
using System.Globalization;
using System.Reflection;

namespace Thomas.Database.Cache.Metadata
{
    public readonly struct MetadataPropertyInfo
    {
        internal MetadataPropertyInfo(in PropertyInfo info)
        {
            PropertyInfo = info;
            SetValue = null;

            if (PropertyInfo.PropertyType.IsGenericType)
            {
                Type = Nullable.GetUnderlyingType(info.PropertyType);
                SetValue = SetNullableValue;
            }
            else
            {
                Type = info.PropertyType;
                SetValue = SetNotNullableValue;
            }
        }

        private readonly PropertyInfo PropertyInfo { get; }
        private readonly Type? Type { get; }

        public delegate void SetValueObject(in object item, in object value, in CultureInfo cultureInfo);
        public readonly SetValueObject SetValue;

        private readonly void SetNullableValue(in object item,in object value,in CultureInfo cultureInfo) => PropertyInfo.SetValue(item, Convert.ChangeType(value, Type!), BindingFlags.GetField | BindingFlags.Public, null, null, cultureInfo);

        private readonly void SetNotNullableValue(in object item,in object value,in CultureInfo cultureInfo) => PropertyInfo.SetValue(item, value, BindingFlags.GetField | BindingFlags.Public, null, null, cultureInfo);

        public readonly object GetValue<T>(in T item)
        {
            return PropertyInfo.GetValue(item) ?? DBNull.Value;
        }

        public readonly string ErrorFormat(in object value)
        {
            var val = GetValue(in value);
            return $"\t" + PropertyInfo.Name + " : " + (val is null ? "NULL" : val) + " ";
        }

    }
}