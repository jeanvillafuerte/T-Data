using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Reflection;
using Thomas.Database.Attributes;
using Thomas.Database.Exceptions;

namespace Thomas.Database.Cache.Metadata
{
    public sealed class MetadataPropertyInfo
    {
        private MetadataPropertyInfo() { }

        public MetadataPropertyInfo(PropertyInfo info)
        {
            PropertyInfo = info;
            Type = PropertyInfo.PropertyType.IsGenericType ? Nullable.GetUnderlyingType(info.PropertyType) : info.PropertyType;
        }

        public MetadataPropertyInfo(PropertyInfo info, DbParameter parameter, int dbType)
        {
            PropertyInfo = info;
            Direction = GetParameterDireccion(PropertyInfo);
            Size = GetParameterSize(PropertyInfo);
            DbParameterName = parameter.ParameterName;
            DbType = dbType;
        }

        public bool IsOutParameter
        {
            get
            {
                return Direction == ParameterDirection.Output || Direction == ParameterDirection.InputOutput;
            }
        }

        private PropertyInfo PropertyInfo { get; }
        private Type? Type { get; }

        public string DbParameterName { get; set; }
        public ParameterDirection Direction { get; set; }
        public int Size { get; set; }
        public int DbType { get; set; }

        public object GetDbParameterValue(object value)
        {
           return PropertyInfo.GetValue(value) ?? DBNull.Value;
        }

        public void SetValue(object item, object value, CultureInfo cultureInfo)
        {
            PropertyInfo.SetValue(item, Convert.ChangeType(value, Type!), BindingFlags.GetField | BindingFlags.Public, null, null, cultureInfo);
        }

        public object GetValue<T>(T item)
        {
            return PropertyInfo.GetValue(item) ?? DBNull.Value;
        }

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

        public string ParameterName
        {
            get
            {
                return PropertyInfo.Name.ToLower();
            }
        }
    }
}