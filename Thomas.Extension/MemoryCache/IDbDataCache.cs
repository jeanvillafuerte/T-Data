using System.Collections.Generic;

namespace Thomas.Cache.MemoryCache
{
    public interface IDbDataCache
    {
        void AddOrUpdate<T>(string hash, IEnumerable<T> result) where T : class, new();
        bool TryGet<T>(string hash, out IEnumerable<T> result) where T : class, new();
        void Release(string hash);
        void Clear();
    }

    public interface IDbParameterCache
    {
        void AddOrUpdate(string hash, object result);
        bool TryGet(string hash, out object? result);
        void Release(string hash);
    }
}
