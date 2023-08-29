using System;
using System.Data;
using System.Globalization;
using System.Reflection;
using Thomas.Database.Attributes;
using Thomas.Database.Exceptions;

namespace Thomas.Database.Cache.Metadata
{
    public sealed class MetadataPropertyInfo
    {
        public MetadataPropertyInfo(PropertyInfo info, Type? type)
        {
            PropertyInfo = info;
            Type = type;
        }

        public bool IsOutParameter
        {
            get
            {
                var direction = GetParameterDireccion();
                return direction == ParameterDirection.Output || direction == ParameterDirection.InputOutput;
            }
        }

        public delegate void SetValueDelegate(object item, object value, CultureInfo cultureInfo);

        private PropertyInfo PropertyInfo { get; }
        private Type? Type { get; }

        public void SetValue(object item, object value, CultureInfo cultureInfo)
        {
            PropertyInfo.SetValue(item, Convert.ChangeType(value, Type!), BindingFlags.GetField | BindingFlags.Public, null, null, cultureInfo);
        }

        public object GetValue<T>(T item)
        {
            return PropertyInfo.GetValue(item) ?? DBNull.Value;
        }

        public int GetParameterSize()
        {
            foreach (var attribute in PropertyInfo.GetCustomAttributes(true))
            {
                var attr = attribute as ParameterSizeAttribute;
                if (attr != null)
                    return attr.Size;
            }

            return 0;
        }

        public ParameterDirection GetParameterDireccion()
        {
            foreach (var attribute in PropertyInfo.GetCustomAttributes(true))
            {
                var attr = attribute as ParameterDirectionAttribute;
                if (attr != null)
                    return GetDirection(attr.Direction);
            }

            return ParameterDirection.Input;
        }

        public static ParameterDirection GetDirection(ParamDirection direction) => direction switch
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

        public string PropertyName
        {
            get
            {
                return Type!.Name.ToLower();
            }
        }
    }
}