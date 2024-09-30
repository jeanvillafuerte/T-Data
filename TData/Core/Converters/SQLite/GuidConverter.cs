using System;

namespace TData.Core.Converters.SQLite
{
    internal class GuidConverter : IInParameterValueConverter, IOutParameterValueConverter
    {
        public Type SourceType => typeof(Guid);
        public Type TargetType => typeof(string);

        bool IInParameterValueConverter.CanConvert(object value)
        {
            return value is Guid;
        }

        bool IOutParameterValueConverter.CanConvert(object value, Type targetType)
        {
            return value is string && targetType == typeof(Guid);
        }

        object IInParameterValueConverter.ConvertInValue(object value)
        {
            return ((Guid)value).ToString();
        }

        object IOutParameterValueConverter.ConvertOutValue(object value)
        {
            return Guid.Parse((string)value);
        }
    }
}
