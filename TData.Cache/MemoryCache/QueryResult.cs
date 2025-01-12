using System;
using System.Linq.Expressions;

namespace TData.Cache.MemoryCache
{
    internal interface IQueryResult
    {
        DateTime? Expiration { get; set; }
        string Query { get; }
        object Params { get; }
        MethodHandled MethodHandled { get; }
        Expression Where { get; }
        Expression Selector { get; }
        bool IsTuple { get; }
        IQueryResult PrepareForCache(TimeSpan ttl);
        object GetSerializedData(in SerializerDelegate serializer);
        IQueryResult PrepareForRefresh(string query);
        void ExpireValue();
    }

    internal sealed class QueryResult<T> : IQueryResult
    {
        public DateTime? Expiration { get; set; }
        public string Query { get; }
        public object Params { get; }
        public T Data { get; }
        public Expression Where { get; }
        public Expression Selector { get; }
        public MethodHandled MethodHandled { get; }

        public bool IsTuple => (int)MethodHandled >= 5;

        public QueryResult() { }

        public QueryResult(in MethodHandled methodHandled, in string query, in object parameters, in T data, in DateTime? expiration = null)
        {
            MethodHandled = methodHandled;
            Query = query;
            Params = parameters;
            Data = data;
            Expiration = expiration;
        }

        public QueryResult(in MethodHandled methodHandled, in string query, in object parameters, in T data, in DateTime? expiration = null, in Expression where = null, in Expression selector = null)
        {
            MethodHandled = methodHandled;
            Query = query;
            Params = parameters;
            Data = data;
            Where = where;
            Selector = selector;
            Expiration = expiration;
        }

        public IQueryResult PrepareForCache(TimeSpan ttl)
        {
           return new QueryResult<T>(MethodHandled, null, Params, default, DateTime.UtcNow.Add(ttl), Where, Selector);
        }

        public object GetSerializedData(in SerializerDelegate serializer)
        {
            return serializer(Data);
        }

        public IQueryResult PrepareForRefresh(string query)
        {
            return new QueryResult<T>(MethodHandled, query, Params, default, Expiration, Where, Selector);
        }

        public void ExpireValue()
        {
            Expiration = DateTime.UtcNow;
        }
    }
}
