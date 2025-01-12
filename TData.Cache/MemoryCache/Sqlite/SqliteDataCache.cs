
namespace TData.Cache.MemoryCache.Sqlite
{
    using System;
    using TData.Core.FluentApi;
    using TData.Configuration;
    using System.IO;
    using System.Threading.Tasks;

    internal sealed class SqliteDataCache : IDbDataCache
    {
        const string TEMPORARY_TABLE_CONFIG = @"DROP TABLE IF EXISTS TDATA_ENTITY_CACHE;
                                                CREATE TABLE TDATA_ENTITY_CACHE(ID INTEGER PRIMARY KEY ASC, QUERY TEXT NULL, CONTENT {0} NOT NULL);
                                                PRAGMA journal_mode = WAL;
                                                PRAGMA page_size = 16384;
                                                PRAGMA cache_size = -2000;
                                                PRAGMA synchronous = NORMAL;
                                                PRAGMA locking_mode = EXCLUSIVE;";

        public bool IsMemoryCache => false;
        private string CacheInstanceName => $"tdata_cache_{_dbIdentifier}";
        public TimeSpan TTL => _inMemoryCache.TTL;

        readonly SerializerDelegate _serializer;
        readonly DeserializeDelegate _deserializer;
        readonly DbDataCache _inMemoryCache;
        readonly string _dbIdentifier;
        readonly bool _isTextFormat;

        private SqliteDataCache(string signature, string id, CacheSettings settings)
        {
            CachedDbHub.CacheDbDictionary.TryGetValue($"{signature}_{id}", out var inMemoryCache);
            _inMemoryCache = (DbDataCache)inMemoryCache;
            _dbIdentifier = signature;
            _serializer = settings.Serializer;
            _deserializer = settings.Deserializer;
            _isTextFormat = settings.IsTextFormat;
        }

        public static void Initialize(in string signature, in CacheSettings configuration)
        {
            var tempCacheName = $"tdata_cache_{signature}";
            DbConfig.Register(new DbSettings(tempCacheName, DbProvider.Sqlite, $"Data Source=.\\tdata_cache_{signature}.db") { BufferSize = 8192 });
            DbHub.Use(tempCacheName).Execute(string.Format(TEMPORARY_TABLE_CONFIG, configuration.IsTextFormat ? "TEXT" : "BLOB"));

            var randomId = Guid.NewGuid().ToString();
            var inMemoryCacheName = $"{signature}_{randomId}";
            DbDataCache.Initialize(inMemoryCacheName, configuration.TTL);
            CachedDbHub.CacheDbDictionary.TryGetValue(inMemoryCacheName, out var inMemoryCache);
            CachedDbHub.CacheDbDictionary.TryAdd(signature, new SqliteDataCache(signature, randomId, configuration));

            var tableBuilder = new TableBuilder();
            DbTable dbTable = tableBuilder.AddTable<SqliteEntity>(x => x.ID);
            dbTable.AddFieldsAsColumns<SqliteEntity>().DbName("TDATA_ENTITY_CACHE");
            dbTable.Column<SqliteEntity>(x => x.CONTENT).ForceCast().LongTextTreatment();
            DbHub.AddTableBuilder(tableBuilder);
        }

        public void AddOrUpdate(in int key, IQueryResult result)
        {
            var context = DbHub.Use(CacheInstanceName);

            context.Execute(@"INSERT INTO TDATA_ENTITY_CACHE(ID, QUERY, CONTENT) VALUES ($ID, $QUERY, $CONTENT) ON CONFLICT(ID)DO UPDATE SET CONTENT = $CONTENT",
                new SqliteEntity { ID = key, QUERY = result.Query, CONTENT = result.GetSerializedData(_serializer) });
            
            _inMemoryCache.AddOrUpdate(key, result.PrepareForCache(_inMemoryCache.TTL));
        }

        public void Clear(in int key)
        {
            _inMemoryCache.TryGetValueForRefresh(in key, out var result);
            result.ExpireValue();
        }

        public void Clear()
        {
            DbHub.Use(CacheInstanceName).Execute(string.Format(TEMPORARY_TABLE_CONFIG, _isTextFormat ? "TEXT" : "BLOB"));
        }

        public bool TryGet<T>(in int key, out QueryResult<T> result)
        {
            var entity = DbHub.Use(CacheInstanceName).FetchOne<SqliteEntity>("SELECT QUERY, CONTENT FROM TDATA_ENTITY_CACHE WHERE ID = $ID", new SqliteSimpleQueryRequest(key));

            if (entity != null && _inMemoryCache.TryGet<T>(key, out var metadata))
            {
                var isList = metadata.MethodHandled == MethodHandled.FetchListExpression || metadata.MethodHandled == MethodHandled.FetchListQueryString;
                var list = (T)_deserializer(entity.CONTENT, typeof(T), isList);
                result = new QueryResult<T>(metadata.MethodHandled, entity.QUERY, metadata.Params, in list, metadata.Expiration, metadata.Where, metadata.Selector);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public bool TryGetValueForRefresh(in int key, out IQueryResult result)
        {
            _inMemoryCache.TryGetValueForRefresh(in key, out result);

            if (result.MethodHandled == MethodHandled.FetchOneExpression || result.MethodHandled == MethodHandled.FetchListExpression)
                return true;

            var entity = DbHub.Use(CacheInstanceName).FetchOne<SqliteEntity>("SELECT QUERY FROM TDATA_ENTITY_CACHE WHERE ID = $ID", new SqliteSimpleQueryRequest(key));

            if (entity != null)
            {
                result = result.PrepareForRefresh(entity.QUERY);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public bool TryGetBytes(in int key, out byte[] data)
        {
            var entity = DbHub.Use(CacheInstanceName).FetchOne<SqliteEntity>("SELECT QUERY, CONTENT FROM TDATA_ENTITY_CACHE WHERE ID = $ID", new SqliteSimpleQueryRequest(key));

            if (entity != null)
            {
                _inMemoryCache.TryGetValueForRefresh(key, out var metadata);
                if (metadata.Expiration < DateTime.UtcNow)
                {
                    data = null;
                    return false;
                }

                data = (byte[])entity.CONTENT;
                return true;
            }

            data = null;
            return false;
        }

        public bool TryGetString(in int key, out string data)
        {
            var entity = DbHub.Use(CacheInstanceName).FetchOne<SqliteEntity>("SELECT QUERY, CONTENT FROM TDATA_ENTITY_CACHE WHERE ID = $ID", new SqliteSimpleQueryRequest(key));

            if (entity != null)
            {
                _inMemoryCache.TryGetValueForRefresh(key, out var metadata);
                if (metadata.Expiration < DateTime.UtcNow)
                {
                    data = null;
                    return false;
                }

                data = (string)entity.CONTENT;
                return true;
            }

            data = null;
            return false;
        }

        public bool CanLoadStream(in int key)
        {
            if (_inMemoryCache.TryGetValueForRefresh(key, out var metadata))
            {
                if (metadata.Expiration < DateTime.UtcNow)
                {
                    return false;
                }

                return true && _isTextFormat;
            }

            return false;
        }

        public void LoadStream(in int key, in StreamWriter stream)
        {
            _inMemoryCache.TryGetValueForRefresh(key, out var metadata);

            if (metadata.Expiration < DateTime.UtcNow)
            {
                throw new InvalidOperationException();
            }

            DbHub.Use(CacheInstanceName).LoadTextStream("SELECT CONTENT FROM TDATA_ENTITY_CACHE WHERE ID = $ID", new SqliteSimpleQueryRequest(key), stream);
        }

        public async Task LoadStreamAsync(int key, StreamWriter stream)
        {
            _inMemoryCache.TryGetValueForRefresh(key, out var metadata);

            if (metadata.Expiration < DateTime.UtcNow)
            {
                throw new InvalidOperationException();
            }

            await DbHub.Use(CacheInstanceName).LoadTextStreamAsync("SELECT CONTENT FROM TDATA_ENTITY_CACHE WHERE ID = $ID", new SqliteSimpleQueryRequest(key), stream);
        }

        class SqliteSimpleQueryRequest
        {
            public int ID { get; }

            public SqliteSimpleQueryRequest(in int id)
            {
                ID = id;
            }
        }

        class SqliteEntity
        {
            public int ID { get; set; }
            public object CONTENT { get; set; }
            public string QUERY { get; set; }
        }
    }
}
