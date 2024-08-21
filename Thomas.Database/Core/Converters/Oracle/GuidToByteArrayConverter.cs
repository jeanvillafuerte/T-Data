using System;

namespace Thomas.Database.Core.Converters.Oracle
{
    public class GuidToByteArrayConverter : IInParameterValueConverter, IOutParameterValueConverter
    {
        public Type SourceType => typeof(Guid);
        public Type TargetType => typeof(byte[]);

        bool IInParameterValueConverter.CanConvert(object value)
        {
            return value is Guid;
        }

        bool IOutParameterValueConverter.CanConvert(object value, Type targetType)
        {
            return targetType == typeof(Guid) && value is byte[];
        }

        object IInParameterValueConverter.ConvertInValue(object value)
        {
            return ((Guid)value).ToByteArray();
        }

        object IOutParameterValueConverter.ConvertOutValue(object value)
        {
            return new Guid((byte[])value);
        }
    }
}
