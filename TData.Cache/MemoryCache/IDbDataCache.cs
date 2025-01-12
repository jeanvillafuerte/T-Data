using System;
using System.IO;
using System.Threading.Tasks;

namespace TData.Cache.MemoryCache
{
    internal interface IDbDataCache
    {
        TimeSpan TTL { get; }
        bool IsMemoryCache { get; }
        void AddOrUpdate(in int key, IQueryResult result);
        void Clear(in int key);
        void Clear();
        bool TryGet<T>(in int key, out QueryResult<T> result);
        bool TryGetValueForRefresh(in int key, out IQueryResult data);
        bool TryGetBytes(in int key, out byte[] data);
        bool TryGetString(in int key, out string data);
        bool CanLoadStream(in int key);
        void LoadStream(in int calculatedHash, in StreamWriter stream);
        Task LoadStreamAsync(int calculatedHash, StreamWriter stream);
    }
}
