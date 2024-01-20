using System;

namespace Thomas.Cache.MemoryCache
{
    internal interface IDictionaryDbQueryItem
    {
        string Identifier { get; set; }
        string Query { get; set; }
        object? Params { get; set; }
    }

    internal class DictionaryDbQueryItem<T> : IDictionaryDbQueryItem
    {
        public string Identifier { get; set; }
        public string Query { get; set; }
        public object? Params { get; set; }
        public T Data { get; set; }

        public DictionaryDbQueryItem(string query, object? parameters, T data)
        {
            Query = query;
            Params = parameters;
            Data = data;
        }
    }
}
