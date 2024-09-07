using System;

namespace Thomas.Database.Core.Converters
{
    public interface IParameterValueConverter
    {
        Type SourceType { get; }
        Type TargetType { get; }
    }

    public interface IInParameterValueConverter : IParameterValueConverter
    {
       
        bool CanConvert(object value);
        object ConvertInValue(object value);
    }

    public interface IOutParameterValueConverter : IParameterValueConverter
    {
        bool CanConvert(object value, Type targetType);
        object ConvertOutValue(object value);
    }
}
