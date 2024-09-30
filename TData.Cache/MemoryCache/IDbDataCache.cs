namespace TData.Cache.MemoryCache
{
    internal interface IDbDataCache
    {
        void AddOrUpdate(in int key, IQueryResult result);
        bool TryGet<T>(in int key, out QueryResult<T> result);
        void Clear(in int key);
        void Clear();
    }
}
