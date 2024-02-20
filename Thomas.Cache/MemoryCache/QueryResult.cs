using System.Linq.Expressions;

namespace Thomas.Cache.MemoryCache
{
    internal interface IQueryResult
    {
        string Identifier { get; set; }
        string Query { get; set; }
        object? Params { get; set; }
        Expression Expression { get; set; }
        MethodHandled MethodHandled { get; set; }
    }

    internal class QueryResult<T> : IQueryResult
    {
        public string Identifier { get; set; }
        public string Query { get; set; }
        public object? Params { get; set; }
        public T Data { get; set; }
        public MethodHandled MethodHandled { get; set; }
        public Expression Expression { get; set; }

        public QueryResult(MethodHandled methodHandled, string query, object? parameters, T data, Expression expression = null)
        {
            MethodHandled = methodHandled;
            Query = query;
            Params = parameters;
            Data = data;
            Expression = expression;
        }
    }
}
