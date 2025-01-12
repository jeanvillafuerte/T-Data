using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace TData.Cache.MemoryCache
{
    internal sealed class DbDataCache : IDbDataCache
    {
        public TimeSpan TTL
        {
            get
            {
                return _ttl;
            }
        }

        readonly TimeSpan _ttl;
        public bool IsMemoryCache => true;

        internal DbDataCache(TimeSpan ttl) 
        { 
            _ttl = ttl.Equals(TimeSpan.MinValue) ? TimeSpan.FromDays(1) : ttl;
        }

        private static ConcurrentDictionary<int, IQueryResult> CacheObject  = new ConcurrentDictionary<int, IQueryResult>();

        public static void Initialize(in string signature, in TimeSpan ttl)
        {
            CachedDbHub.CacheDbDictionary.TryAdd(signature, new DbDataCache(ttl));
        }

        public void AddOrUpdate(in int key, IQueryResult result)
        {
            CacheObject.AddOrUpdate(key, result, (k, v) => result.PrepareForCache(_ttl));
        }

        public bool TryGetValueForRefresh(in int key, out IQueryResult data) => CacheObject.TryGetValue(key, out data);

        public bool TryGet<T>(in int key, out QueryResult<T> result)
        {
            if (CacheObject.TryGetValue(key, out IQueryResult data) && data is QueryResult<T> convertedValue)
            {
                if (data.Expiration < DateTime.UtcNow)
                {
                    result = null;
                    return false;
                }

                result = convertedValue;
                return true;
            }

            result = null;
            return false;
        }

        public void Clear(in int hash)
        {
            CacheObject.TryRemove(hash, out var _);
        }

        public void Clear()
        {
            CacheObject.Clear();
        }

        public bool TryGetBytes(in int key, out byte[] data)
        {
            throw new System.NotImplementedException();
        }

        public bool TryGetString(in int key, out string data)
        {
            throw new System.NotImplementedException();
        }

        public bool CanLoadStream(in int key)
        {
            throw new NotImplementedException();
        }

        public void LoadStream(in int calculatedHash, in StreamWriter stream)
        {
            throw new NotImplementedException();
        }

        public Task LoadStreamAsync(int calculatedHash, StreamWriter stream)
        {
            throw new NotImplementedException();
        }
    }

}

