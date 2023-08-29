using System.Collections.Generic;

namespace Thomas.Cache.Manager
{
    public interface IDbCacheManager
    {
        //void Add<T>(int hash, IEnumerable<T> result);
        void Add<T>(int hash, IEnumerable<T> result) where T : class, new();
        bool TryGet<T>(int hash, out IEnumerable<T> result) where T : class, new();
        //bool TryGet<T>(int hash, out IEnumerable<T> result) where T : class, new();
        void Release(int hash);
        void Clear();
    }
}
