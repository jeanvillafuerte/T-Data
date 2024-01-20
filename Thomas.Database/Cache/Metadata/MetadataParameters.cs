using System;
using System.Data;
using System.Globalization;
using System.Reflection;
using Thomas.Database.Attributes;
using Thomas.Database.Exceptions;

namespace Thomas.Database.Cache.Metadata
{
    public sealed class MetadataParameters<T> where T : Enum
    {
        private PropertyInfo PropertyInfo { get; }
        private Type? Type { get; }

        public T DbType { get; }
        public int Size { get; }
        public ParameterDirection Direction { get; }
        public string DbParameterName { get; }
        public bool IsInParameter
        {
            get
            {
                return Direction == ParameterDirection.Input || Direction == ParameterDirection.InputOutput;
            }

        }

        public bool IsOutParameter
        {
            get
            {
                return Direction == ParameterDirection.Output || Direction == ParameterDirection.InputOutput;
            }
        }

        public delegate void SetValueObject(in object item, in object value, in CultureInfo cultureInfo);
        public SetValueObject? SetValue;

        //add by ref property that are byVal
        //add delegate to implement not null and nullable SetValue

        internal MetadataParameters(in PropertyInfo info, in string name, in T dbType)
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
        }

        public object GetValue(in object value)
        {
            return PropertyInfo.GetValue(value) ?? DBNull.Value;
        }

        private void SetNullableValue(in object item, in object value, in CultureInfo cultureInfo) => PropertyInfo.SetValue(item, Convert.ChangeType(value, Type!), BindingFlags.GetField | BindingFlags.Public, null, null, cultureInfo);

        private void SetNotNullableValue(in object item, in object value, in CultureInfo cultureInfo) => PropertyInfo.SetValue(item, value, BindingFlags.GetField | BindingFlags.Public, null, null, cultureInfo);

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
