using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Thomas.Cache")]
namespace Thomas.Database.Cache
{
    internal sealed class DynamicQueryInfo
    {
        private static ConcurrentDictionary<int, ExpressionQueryItem> DynamicQueryStringDictionary = new ConcurrentDictionary<int, ExpressionQueryItem>(Environment.ProcessorCount * 2, 50);

        private DynamicQueryInfo() { }

        internal static void Set(in int key, ExpressionQueryItem value) => DynamicQueryStringDictionary.TryAdd(key, value);
        internal static bool TryGet(in int key, out ExpressionQueryItem meta) => DynamicQueryStringDictionary.TryGetValue(key, out meta);
        public static void Clear()
        {
            DynamicQueryStringDictionary.Clear();
            DynamicQueryStringDictionary = new ConcurrentDictionary<int, ExpressionQueryItem>(Environment.ProcessorCount * 2, 50);
        }
    }

    internal sealed class ExpressionQueryItem
    {
        public string Query { get; set; }
        public bool IsStaticQuery { get; set; }

        /// <summary>
        /// Hold parameter values on static queries
        /// </summary>
        public object[] ParameterValues { get; set; }

        public ExpressionQueryItem(in string query, in bool isStaticQuery, in object[] parameterValues)
        {
            Query = query;
            IsStaticQuery = isStaticQuery;
            ParameterValues = parameterValues;
        }
    }
}
