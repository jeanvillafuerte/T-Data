using System.Linq.Expressions;

namespace TData.Cache.MemoryCache
{
    internal interface IQueryResult
    {
        string Query { get; }
        object Params { get; }
        MethodHandled MethodHandled { get; }
        Expression Where { get; }
        Expression Selector { get; }
        bool IsTuple { get; }
    }

    internal sealed class QueryResult<T> : IQueryResult
    {
        public string Query { get; }
        public object Params { get; }
        public T Data { get; }
        public Expression Where { get; }
        public Expression Selector { get; }
        public MethodHandled MethodHandled { get; }

        public bool IsTuple => (int)MethodHandled >= 5;

        public QueryResult(in MethodHandled methodHandled, in string query, in object parameters, in T data)
        {
            MethodHandled = methodHandled;
            Query = query;
            Params = parameters;
            Data = data;
        }

        public QueryResult(in MethodHandled methodHandled, in string query, in object parameters, in T data, in Expression where = null, in Expression selector = null)
        {
            MethodHandled = methodHandled;
            Query = query;
            Params = parameters;
            Data = data;
            Where = where;
            Selector = selector;
        }
    }
}
