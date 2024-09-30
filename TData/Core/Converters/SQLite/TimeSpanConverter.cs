using System;

namespace TData.Core.Converters.SQLite
{
    internal class TimeSpanConverter : IInParameterValueConverter, IOutParameterValueConverter
    {
        public Type SourceType => typeof(TimeSpan);
        public Type TargetType => typeof(long);

        public bool CanConvert(object value) => value is TimeSpan;

        public object ConvertInValue(object value) => ((TimeSpan)value).Ticks;

        bool IOutParameterValueConverter.CanConvert(object value, Type targetType) => targetType == typeof(TimeSpan) && (value is long || value is int);

        object IOutParameterValueConverter.ConvertOutValue(object value) => TimeSpan.FromTicks((long)value);
    }
}
