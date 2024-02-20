using System;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Thomas.Database.Attributes;
using Thomas.Database.Exceptions;

namespace Thomas.Database.Cache
{
    internal struct MetadataParameters
    {
        private PropertyInfo PropertyInfo { get; }
        private Type? Type { get; }
        public string DbTypeName { get; }
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

        internal MetadataParameters(in PropertyInfo propertyInfo, in string name, in string dbTypeName)
        {
            DbParameterName = name;
            DbTypeName = dbTypeName;
            Size = GetParameterSize(propertyInfo);
            Direction = GetParameterDireccion(propertyInfo);
            PropertyInfo = propertyInfo;
            Type = null;
            SetValue = null;

            if (Direction == ParameterDirection.Output || Direction == ParameterDirection.InputOutput)
            {
                SetValue = SetInternalValue;

                if (propertyInfo.PropertyType.IsGenericType)
                    Type = Nullable.GetUnderlyingType(propertyInfo.PropertyType);
                else
                    Type = propertyInfo.PropertyType;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetValue(in object value)
        {
            var getter = PropertyInfo.GetMethod!.CreateDelegate(typeof(Func<,>).MakeGenericType(PropertyInfo.DeclaringType!, PropertyInfo.PropertyType));
            return getter.DynamicInvoke(value) ?? DBNull.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetInternalValue(in object item, in object value, in CultureInfo cultureInfo)
        {
            var setter = PropertyInfo.SetMethod!.CreateDelegate(typeof(Action<,>).MakeGenericType(PropertyInfo.DeclaringType!, PropertyInfo.PropertyType));
            setter.DynamicInvoke(item, Convert.ChangeType(value, Type!, cultureInfo));
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
