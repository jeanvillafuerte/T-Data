namespace Thomas.Cache.MemoryCache
{
    internal interface IDbDataCache
    {
        void AddOrUpdate(ulong key, IQueryResult result);
        bool TryGet<T>(ulong key, out QueryResult<T>? result);
        void Clear(ulong key);
        void Clear();
    }

    internal interface IDbParameterCache
    {
        void AddOrUpdate(ulong key, object result);
        bool TryGet(ulong key, out object? result);
        void Clear(ulong key);
    }
}
