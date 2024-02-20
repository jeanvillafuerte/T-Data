namespace Thomas.Cache.MemoryCache
{
    internal interface IDbDataCache
    {
        void AddOrUpdate(string hash, IQueryResult result);
        bool TryGet<T>(string hash, out QueryResult<T>? result);
        void Release(string hash);
        void Release();
    }

    internal interface IDbParameterCache
    {
        void AddOrUpdate(string hash, object result);
        bool TryGet(string hash, out object? result);
        void Release(string hash);
    }
}
