using System.Collections.Generic;

namespace Thomas.Cache.Manager
{
    public interface IDbCacheManager
    {
        void Add<T>(uint hash, IEnumerable<T> result) where T : class, new();
        bool TryGet<T>(uint hash, out IEnumerable<T> result) where T : class, new();
        void Release(uint hash);
        void Clear();
    }
}
