using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Thomas.Cache.Exceptions;

namespace Thomas.Cache.Manager
{
    internal class DbCacheManager : IDbCacheManager
    {
        private static DbCacheManager instance;
        public static DbCacheManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DbCacheManager();
                }
                return instance;
            }
        }

        private DbCacheManager() { }

        private static ConcurrentDictionary<int, object> CacheObject { get; set; } = new ConcurrentDictionary<int, object>();

        public void Add<T>(int hash, IEnumerable<T> result) where T : class, new()  => CacheObject.TryAdd(hash, result);

        public bool TryGet<T>(int hash, out T result) where T : class, new()
        {
            if (CacheObject.TryGetValue(hash, out object? data))
            {
                if (data is T convertedValue)
                {
                    result = convertedValue;
                    return true;
                }
            }

            result = new T();
            return false;
        }

        public bool TryGet<T>(int hash, out IEnumerable<T> result) where T : class, new()
        {
            if (CacheObject.TryGetValue(hash, out object? data))
            {
                if (data is IEnumerable<T> convertedValue)
                {
                    result = convertedValue;
                    return true;
                }
            }

            result = Enumerable.Empty<T>();
            return false;
        }

        public void Release(int hash) => CacheObject.TryRemove(hash, out var _);

        public void Clear() => CacheObject.Clear();
    }

    internal class DbCompressedCacheManager : IDbCacheManager
    {
        private static DbCompressedCacheManager instance;
        public static DbCompressedCacheManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DbCompressedCacheManager();
                }
                return instance;
            }
        }

        private DbCompressedCacheManager() { }

        private static ConcurrentDictionary<int, Lazy<byte[]>> CacheObject { get; set; } = new ConcurrentDictionary<int, Lazy<byte[]>>();

        private Lazy<byte[]> GetOrAdd<T>(int hash, IEnumerable<T> value) where T: class, new()
        {
            if (value == null)
                throw new NotNullValueAllowedException("Cache cannot storage a null value");

            return CacheObject.GetOrAdd(hash, x =>
                new Lazy<byte[]>(
                    () =>
                    {
                        byte[] bytes;
                        BinaryFormatter formatter = new BinaryFormatter();
                        using (MemoryStream stream = new MemoryStream())
                        {
                            formatter.Serialize(stream, value);
                            bytes = stream.ToArray();
                        }

                        using (MemoryStream compressedStream = new MemoryStream())
                        {
                            using (GZipStream compressionStream = new GZipStream(compressedStream, CompressionMode.Compress))
                                compressionStream.Write(bytes, 0, bytes.Length);

                            return compressedStream.ToArray();
                        }

                    },
                    LazyThreadSafetyMode.ExecutionAndPublication)
            );
        }

        public void Add<T>(int hash, IEnumerable<T> value) where T : class, new() => GetOrAdd<T>(hash, value); 

        public bool TryGet<T>(int hash, out IEnumerable<T> result) where T : class, new()
        {
            if (CacheObject.TryGetValue(hash, out var data))
            {
                using (MemoryStream decompressedStream = new MemoryStream())
                {
                    using (GZipStream decompressionStream = new GZipStream(new MemoryStream(data.Value), CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedStream);
                    }

                    byte[] decompressedBytes = decompressedStream.ToArray();

                    var formatter = new BinaryFormatter();
                    using (MemoryStream stream = new MemoryStream(decompressedBytes))
                    {
                        result = (formatter.Deserialize(stream) as T[])!;
                    }
                }
            }
            else
                result = null;

            return false;
        }

        public void Release(int hash) => CacheObject.TryRemove(hash, out var _);

        public void Clear() => CacheObject.Clear();

    }

}

