using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Thomas.Cache.Manager;
using Thomas.Database;
using Thomas.Database.Cache.Metadata;
using Thomas.Database.Core;

namespace Thomas.Cache
{
    public interface ICachedDatabase : IDbResulSet
    {
        void ReleaseCache();
        void ReleaseResult(string script);
        void ReleaseResult(string script, object inputData);
    }

    internal sealed class CachedDatabase : ICachedDatabase
    {
        private readonly IDbCacheManager _cache;
        private readonly IDatabase _database;
        private readonly string _signature;
        private readonly CultureInfo _culture;

        internal CachedDatabase(IDbCacheManager cache, IDatabase database, string signature, CultureInfo culture)
        {
            _cache = cache;
            _database = database;
            _signature = signature;
            _culture = culture;
        }

        private static bool CheckIfAnonymousType(Type type)
        {
            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                && type.IsGenericType && type.Name.Contains("AnonymousType")
                && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                && type.Attributes.HasFlag(TypeAttributes.NotPublic);
        }

        private void CopyPropertyValues(object source, object dest)
        {
            var typeName = source.GetType().FullName;

            if (MetadataCacheManager.Instance.TryGet(typeName!, out var meta))
            {
                foreach (var item in meta)
                {
                    if (item.Value.IsOutParameter)
                    {
                        var value = item.Value.GetValue(source);
                        item.Value.SetValue(dest, value, _culture);
                    }
                }
            }
        }

        private int GenerateUniqueHash(string value)
        {
            int hash = 23;
            hash = hash * 31 + value.GetHashCode();
            hash = hash * 31 + _signature.GetHashCode();
            return hash;
        }

        private int GenerateUniqueHash(string value, object inputData)
        {
            int hash = 23;
            hash = hash * 31 + value.GetHashCode();

            string jsonString = JsonSerializer.Serialize(inputData);
            hash = hash * 31 + jsonString.GetHashCode();
            hash = hash * 31 + _signature.GetHashCode();
            return hash;
        }

        private int GenerateUniqueHashForInput(string value, object inputData)
        {
            int hash = 19;
            hash = hash * 19 + value.GetHashCode();

            string jsonString = JsonSerializer.Serialize(inputData);
            hash = hash * 19 + jsonString.GetHashCode();
            hash = hash * 19 + _signature.GetHashCode();
            hash = hash * 19 + "input".GetHashCode();
            return hash;
        }

        public IEnumerable<T> ToList<T>(string script, bool isStoreProcedure = true) where T : class, new()
        {
            var identifier = GenerateUniqueHash(script);

            if (!_cache.TryGet<T>(identifier, out IEnumerable<T> result))
            {
                result = _database.ToList<T>(script, isStoreProcedure).ToArray();
                _cache.Add(identifier, result);
            }

            return result;
        }

        public IEnumerable<T> ToList<T>(object inputData, string procedureName) where T : class, new()
        {
            var identifier = GenerateUniqueHash(procedureName, inputData);
            var identifierForInput = GenerateUniqueHashForInput(procedureName, inputData);

            var isAnonymousType = CheckIfAnonymousType(inputData.GetType());

            if (!_cache.TryGet<T>(identifier, out IEnumerable<T> result))
            {
                result = _database.ToList<T>(inputData, procedureName);
                _cache.Add(identifier, result);
                if (!isAnonymousType)
                    _cache.Add(identifierForInput, new [] { inputData } );
            }
            else
            {
                if (!isAnonymousType)
                {
                    _cache.TryGet(identifierForInput, out IEnumerable<object> value);
                    CopyPropertyValues(value.First(), inputData);
                }
            }

            return result;
        }

        //public DataBaseOperationResult<IEnumerable<T>> ToListOp<T>(string script, bool isStoreProcedure = true) where T : class, new()
        //{
        //    var identifier = GenerateUniqueHashFromString(script);

        //    if (!_cache.TryGet(identifier, out DataBaseOperationResult<IEnumerable<T>> result))
        //    {
        //        result = _database.ToListOp<T>(script, isStoreProcedure);
        //        _cache.Add(identifier, result);
        //    }

        //    return result;
        //}

        //public DataBaseOperationResult<IEnumerable<T>> ToListOp<T>(object inputData, string procedureName) where T : class, new()
        //{
        //    var identifier = GenerateUniqueHashFromString(procedureName, inputData);

        //    if (!_cache.TryGet(identifier, out DataBaseOperationResult<IEnumerable<T>> result))
        //    {
        //        result = _database.ToListOp<T>(inputData, procedureName);
        //        _cache.Add(identifier, result);
        //    }

        //    return result;
        //}

        public T? ToSingle<T>(string script, bool isStoreProcedure = true) where T : class, new()
        {
            var identifier = GenerateUniqueHash(script);

            if (!_cache.TryGet(identifier, out IEnumerable<T> result))
            {
                var singleResult = _database.ToSingle<T>(script, isStoreProcedure);
                result = new[] { singleResult };
                _cache.Add<T>(identifier, result);
            }

            return result.FirstOrDefault();
        }

        public T? ToSingle<T>(object inputData, string procedureName) where T : class, new()
        {
            var identifier = GenerateUniqueHash(procedureName, inputData);

            if (!_cache.TryGet<T>(identifier, out IEnumerable<T> result))
            {
                var singleResult = _database.ToSingle<T>(inputData, procedureName);
                result = new[] { singleResult };
                _cache.Add(identifier, result);
            }

            return result.FirstOrDefault();
        }

        //public DataBaseOperationResult<T> ToSingleOp<T>(string script, bool isStoreProcedure = true) where T : class, new()
        //{
        //    var identifier = GenerateUniqueHashFromString(script);

        //    if (!_cache.TryGet<T>(identifier, out DataBaseOperationResult<T> result))
        //    {
        //        result = _database.ToSingleOp<T>(script, isStoreProcedure);
        //        _cache.Add(identifier, result);
        //    }

        //    return result;
        //}

        //public DataBaseOperationResult<T> ToSingleOp<T>(object inputData, string procedureName) where T : class, new()
        //{
        //    var identifier = GenerateUniqueHashFromString(procedureName, inputData);

        //    if (!_cache.TryGet<T>(identifier, out DataBaseOperationResult<T> result))
        //    {
        //        result = _database.ToSingleOp<T>(inputData, procedureName);
        //        _cache.Add(identifier, result);
        //    }

        //    return result;
        //}

        public void ReleaseResult(string script)
        {
            var identifier = GenerateUniqueHash(script);
            _cache.Release(identifier);
        }

        public void ReleaseResult(string script, object inputData)
        {
            var identifier = GenerateUniqueHash(script, inputData);
            var identifierForInput = GenerateUniqueHashForInput(script, inputData);
            _cache.Release(identifier);
            _cache.Release(identifierForInput);
        }

        public void ReleaseCache()
        {
            _cache.Clear();
        }

        public async Task<T?> ToSingleAsync<T>(string script, bool isStoreProcedure, CancellationToken cancellationToken) where T : class, new()
        {
            var identifier = GenerateUniqueHash(script);

            if (!_cache.TryGet(identifier, out IEnumerable<T> result))
            {
                var singleResult = await _database.ToSingleAsync<T>(script, isStoreProcedure, cancellationToken);
                result = new[] { singleResult };
                _cache.Add(identifier, result);
            }

            return result.FirstOrDefault();
        }

        public async Task<T?> ToSingleAsync<T>(object inputData, string procedureName, CancellationToken cancellationToken) where T : class, new()
        {
            var identifier = GenerateUniqueHash(procedureName, inputData);

            if (!_cache.TryGet(identifier, out IEnumerable<T> result))
            {
                var singleResult = await _database.ToSingleAsync<T>(inputData, procedureName, cancellationToken);
                result = new[] { singleResult };
                _cache.Add(identifier, result);
            }

            return result.FirstOrDefault();
        }

        public async Task<IEnumerable<T>> ToListAsync<T>(string script, bool isStoreProcedure, CancellationToken cancellationToken) where T : class, new()
        {
            var identifier = GenerateUniqueHash(script);

            if (!_cache.TryGet<T>(identifier, out IEnumerable<T> result))
            {
                result = await _database.ToListAsync<T>(script, isStoreProcedure, cancellationToken);
                _cache.Add(identifier, result);
            }

            return result;
        }

        public async Task<IEnumerable<T>> ToListAsync<T>(object inputData, string procedureName, CancellationToken cancellationToken) where T : class, new()
        {
            var identifier = GenerateUniqueHash(procedureName, inputData);
            var identifierForInput = GenerateUniqueHashForInput(procedureName, inputData);

            var isAnonymousType = CheckIfAnonymousType(inputData.GetType());

            if (!_cache.TryGet<T>(identifier, out IEnumerable<T> result))
            {
                result = await _database.ToListAsync<T>(inputData, procedureName, cancellationToken);
                _cache.Add(identifier, result);
                if (!isAnonymousType)
                    _cache.Add(identifierForInput, new[] { inputData });
            }
            else
            {
                if (!isAnonymousType)
                {
                    _cache.TryGet(identifierForInput, out IEnumerable<object> value);
                    CopyPropertyValues(value.First(), inputData);
                }
            }

            return result;
        }
    }
}
