using System.Linq.Expressions;
using Thomas.Database;

namespace Thomas.Cache.MemoryCache
{
    internal interface IQueryResult
    {
        string Query { get; set; }
        object? Params { get; set; }
        MethodHandled MethodHandled { get; set; }
        Expression? Where { get; set; }
    }

    internal class QueryResult<T> : IQueryResult
    {
        public string Query { get; set; }
        public object? Params { get; set; }
        public T Data { get; set; }
        public Expression? Where { get; set; }
        public MethodHandled MethodHandled { get; set; }

        public QueryResult(MethodHandled methodHandled, string query, object? parameters, T data)
        {
            MethodHandled = methodHandled;
            Query = query;
            Params = parameters;
            Data = data;
        }

        public QueryResult(MethodHandled methodHandled, string query, object? parameters, T data, Expression where = null)
        {
            MethodHandled = methodHandled;
            Query = query;
            Params = parameters;
            Data = data;
            Where = where;
        }
    }
}
