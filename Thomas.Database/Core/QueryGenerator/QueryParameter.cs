using System;

namespace Thomas.Database.Core.QueryGenerator
{
    public class QueryParameter
    {
        public object Value { get; }
        public bool IsOutParam { get; set; }
        public Type SourceType { get; set; }

        public QueryParameter(object value, bool isOutParam, Type targetType)
        {
            Value = value;
            IsOutParam = isOutParam;
            SourceType = targetType;
        }
    }
}
