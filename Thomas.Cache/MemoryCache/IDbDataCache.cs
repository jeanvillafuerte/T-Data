namespace Thomas.Cache.MemoryCache
{
    internal interface IDbDataCache
    {
        void AddOrUpdate(int key, IQueryResult result);
        bool TryGet<T>(int key, out QueryResult<T>? result);
        void Clear(int key);
        void Clear();
    }
}
