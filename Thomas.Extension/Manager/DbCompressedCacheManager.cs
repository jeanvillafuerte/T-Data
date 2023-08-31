using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Thomas.Cache.Exceptions;

namespace Thomas.Cache.Manager
{
    public sealed class DbCompressedCacheManager : IDbCacheManager
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

        private static ConcurrentDictionary<uint, Lazy<byte[]>> CacheObject { get; set; } = new ConcurrentDictionary<uint, Lazy<byte[]>>();

        private Lazy<byte[]> GetOrAdd<T>(uint hash, IEnumerable<T> value) where T : class, new()
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

        public void Add<T>(uint hash, IEnumerable<T> value) where T : class, new() => GetOrAdd<T>(hash, value);

        public bool TryGet<T>(uint hash, out IEnumerable<T> result) where T : class, new()
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

        public void Release(uint hash) => CacheObject.TryRemove(hash, out var _);

        public void Clear() => CacheObject.Clear();

    }
}
