using System;
using System.Data;
using System.Reflection;
using System.Globalization;
using Thomas.Database.Attributes;
using Thomas.Database.Exceptions;

namespace Thomas.Database.Cache.Metadata
{
    public readonly struct MetadataParameters<T> where T : Enum
    {
        private readonly PropertyInfo PropertyInfo { get; }
        private readonly Type? Type { get; }

        public readonly T DbType { get; }
        public readonly int Size { get; }
        public readonly ParameterDirection Direction { get; }
        public readonly string DbParameterName { get; }
        public readonly bool IsInParameter
        {
            get
            {
                return Direction == ParameterDirection.Input || Direction == ParameterDirection.InputOutput;
            }

        }

        public readonly bool IsOutParameter
        {
            get
            {
                return Direction == ParameterDirection.Output || Direction == ParameterDirection.InputOutput;
            }
        }

        public delegate void SetValueObject(in object item, in object value, in CultureInfo cultureInfo);
        public readonly SetValueObject? SetValue;

        //add by ref property that are byVal
        //add delegate to implement not null and nullable SetValue

        internal MetadataParameters(in PropertyInfo info,in string name,in T dbType)
        {
            DbParameterName = name;
            DbType = dbType;
            Size = GetParameterSize(info);
            Direction = GetParameterDireccion(info);
            Type = null;
            PropertyInfo = info;
            SetValue = null;

            if (Direction == ParameterDirection.Output || Direction == ParameterDirection.InputOutput)
            {
                if(PropertyInfo.PropertyType.IsGenericType)
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
        }

        public readonly object GetValue(in object value)
        {
            return PropertyInfo.GetValue(value) ?? DBNull.Value;
        }

        private readonly void SetNullableValue(in object item, in object value, in CultureInfo cultureInfo) => PropertyInfo.SetValue(item, Convert.ChangeType(value, Type!), BindingFlags.GetField | BindingFlags.Public, null, null, cultureInfo);

        private readonly void SetNotNullableValue(in object item, in object value, in CultureInfo cultureInfo) => PropertyInfo.SetValue(item, value, BindingFlags.GetField | BindingFlags.Public, null, null, cultureInfo);

        static ParameterDirection GetParameterDireccion(PropertyInfo property)
        {
            foreach (var attribute in property.GetCustomAttributes(true))
            {
                var attr = attribute as ParameterDirectionAttribute;
                if (attr != null)
                    return GetDirection(attr.Direction);
            }

            return ParameterDirection.Input;
        }

        static ParameterDirection GetDirection(ParamDirection direction) => direction switch
        {
            ParamDirection.Input => ParameterDirection.Input,
            ParamDirection.InputOutput => ParameterDirection.InputOutput,
            ParamDirection.Output => ParameterDirection.Output,
            _ => throw new UnknownParameterDirectionException()
        };

        static int GetParameterSize(PropertyInfo property)
        {
            foreach (var attribute in property.GetCustomAttributes(true))
            {
                var attr = attribute as ParameterSizeAttribute;
                if (attr != null)
                    return attr.Size;
            }

            return 0;
        }
    }
}
