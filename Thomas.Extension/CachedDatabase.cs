using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Thomas.Cache.Helpers;
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

        public IEnumerable<T> ToList<T>(string script, bool isStoreProcedure = true) where T : class, new()
        {
            var identifier = HashHelper.GenerateUniqueHash(script, _signature);

            if (!_cache.TryGet<T>(identifier, out IEnumerable<T> result))
            {
                result = _database.ToList<T>(script, isStoreProcedure).ToArray();
                _cache.Add(identifier, result);
            }

            return result;
        }

        public IEnumerable<T> ToList<T>(object inputData, string procedureName) where T : class, new()
        {
            var identifier = HashHelper.GenerateUniqueHash(procedureName, _signature, inputData);
            var identifierForInput = HashHelper.GenerateUniqueHash(procedureName, _signature, inputData, TypeCacheObject.Input);

            var isAnonymousType = CheckIfAnonymousType(inputData.GetType());

            if (!_cache.TryGet<T>(identifier, out IEnumerable<T> result))
            {
                result = _database.ToList<T>(inputData, procedureName);
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

        public T? ToSingle<T>(string script, bool isStoreProcedure = true) where T : class, new()
        {
            var identifier = HashHelper.GenerateUniqueHash(script, _signature);

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
            var identifier = HashHelper.GenerateUniqueHash(procedureName, _signature, inputData);

            if (!_cache.TryGet<T>(identifier, out IEnumerable<T> result))
            {
                var singleResult = _database.ToSingle<T>(inputData, procedureName);
                result = new[] { singleResult };
                _cache.Add(identifier, result);
            }

            return result.FirstOrDefault();
        }

        public void ReleaseResult(string script)
        {
            var identifier = HashHelper.GenerateUniqueHash(script, _signature);
            _cache.Release(identifier);
        }

        public void ReleaseResult(string script, object inputData)
        {
            var identifier = HashHelper.GenerateUniqueHash(script, _signature, inputData);
            var identifierForInput = HashHelper.GenerateUniqueHash(script, _signature, inputData, TypeCacheObject.Input);
            _cache.Release(identifier);
            _cache.Release(identifierForInput);
        }

        public void ReleaseCache()
        {
            _cache.Clear();
        }

        public async Task<T?> ToSingleAsync<T>(string script, bool isStoreProcedure, CancellationToken cancellationToken) where T : class, new()
        {
            var identifier = HashHelper.GenerateUniqueHash(script, _signature);

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
            var identifier = HashHelper.GenerateUniqueHash(procedureName, _signature, inputData);

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
            var identifier = HashHelper.GenerateUniqueHash(script, _signature);

            if (!_cache.TryGet<T>(identifier, out IEnumerable<T> result))
            {
                result = await _database.ToListAsync<T>(script, isStoreProcedure, cancellationToken);
                _cache.Add(identifier, result);
            }

            return result;
        }

        public async Task<IEnumerable<T>> ToListAsync<T>(object inputData, string procedureName, CancellationToken cancellationToken) where T : class, new()
        {
            var identifier = HashHelper.GenerateUniqueHash(procedureName, _signature, inputData);
            var identifierForInput = HashHelper.GenerateUniqueHash(procedureName, _signature, inputData, TypeCacheObject.Input);

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

        public Tuple<IEnumerable<T1>, IEnumerable<T2>> ToTuple<T1, T2>(string script, bool isStoreProcedure = true)
            where T1 : class, new()
            where T2 : class, new()
        {
            throw new NotImplementedException();
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>> ToTuple<T1, T2, T3>(string script, bool isStoreProcedure = true)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
        {
            throw new NotImplementedException();
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>> ToTuple<T1, T2, T3, T4>(string script, bool isStoreProcedure = true)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
        {
            throw new NotImplementedException();
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>> ToTuple<T1, T2, T3, T4, T5>(string script, bool isStoreProcedure = true)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
        {
            throw new NotImplementedException();
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>> ToTuple<T1, T2, T3, T4, T5, T6>(string script, bool isStoreProcedure = true)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
        {
            throw new NotImplementedException();
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>> ToTuple<T1, T2, T3, T4, T5, T6, T7>(string script, bool isStoreProcedure = true)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
            where T7 : class, new()
        {
            throw new NotImplementedException();
        }

        public Task<Tuple<IEnumerable<T1>, IEnumerable<T2>>> ToTupleAsync<T1, T2>(string script, bool isStoreProcedure, CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new()
        {
            throw new NotImplementedException();
        }

        public Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>> ToTupleAsync<T1, T2, T3>(string script, bool isStoreProcedure, CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
        {
            throw new NotImplementedException();
        }
    }
}
