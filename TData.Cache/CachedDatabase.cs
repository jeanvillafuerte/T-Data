using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using TData.Cache.Helpers;
using TData.Cache.MemoryCache;
using TData.Core.QueryGenerator;

namespace TData.Cache
{
    public interface ICachedDatabase : IDbResultCachedSet
    {
        void Clear();
        void Clear(in string key);
        void Refresh(in string key, in bool throwErrorIfNotFound = false);
        bool CanLoadStream(in string key);
        void LoadStream(in string key, in StreamWriter stream);
        Task LoadStreamAsync(string key, StreamWriter stream);
        bool TryGetStringValue(in string key, out string data);
        bool TryGetBytesValue(in string key, out byte[] data);
    }

    internal sealed class CachedDatabase : ICachedDatabase
    {
        private readonly IDbDataCache _cache;
        private readonly Lazy<IDatabase> _database;
        private readonly ISqlFormatter _sqlValues;
        private readonly TimeSpan _ttl;

        internal CachedDatabase(IDbDataCache cache, Lazy<IDatabase> database, ISqlFormatter sqlValues, in TimeSpan ttl)
        {
            _cache = cache;
            _database = database;
            _sqlValues = sqlValues;
            _ttl = ttl;
        }

        private int CalculateHash(in string script, in object parameters, in string key)
        {
            int _operationKey = 17;

            if (string.IsNullOrEmpty(key))
            {
                unchecked
                {
                    _operationKey = (_operationKey * 23) + script.GetHashCode();
                    _operationKey = (_operationKey * 23) + _sqlValues.Provider.GetHashCode();
                    _operationKey = (_operationKey * 23) + (parameters?.GetHashCode() ?? 0);
                }
            }
            else
            {
                _operationKey = HashHelper.GenerateHash(key);
            }

            return _operationKey;
        }

        #region Single

        public T FetchOne<T>(in string script, in object parameters = null, in string key = null, in bool refresh = false)
        {
            int calculatedKey = CalculateHash(in script, in parameters, in key);
            var fromCache = _cache.TryGet(calculatedKey, out QueryResult<T> result);

            if (!fromCache || refresh)
            {
                var data = _database.Value.FetchOne<T>(in script, in parameters);
                result = new QueryResult<T>(MethodHandled.FetchOneQueryString, in script, in parameters, in data, DateTime.UtcNow.Add(_ttl));
                _cache.AddOrUpdate(in calculatedKey, result);
            }

            return result.Data;
        }

        public T FetchOne<T>(in Expression<Func<T, bool>> where = null, in Expression<Func<T, object>> selector = null, in string key = null, in bool refresh = false)
        {
            var calculatedKey = SQLGenerator<T>.CalculateExpressionKey(in where, in selector, null, typeof(T), SqlOperation.SelectSingle, _sqlValues.Provider, in key);
            var fromCache = _cache.TryGet(in calculatedKey, out QueryResult<T> result);

            if (!fromCache || refresh)
                result = FetchOne(in calculatedKey, in where, in selector);

            return result.Data;
        }

        private QueryResult<T> FetchOne<T>(in int calculatedKey, in Expression<Func<T, bool>> where, in Expression<Func<T, object>> selector)
        {
            var data = _database.Value.FetchOne<T>(where, selector);
            var result = new QueryResult<T>(MethodHandled.FetchOneExpression, null, null, in data, DateTime.UtcNow.Add(_ttl), where, selector);
            _cache.AddOrUpdate(calculatedKey, result);
            return result;
        }

        #endregion

        #region List

        public List<T> FetchList<T>(in Expression<Func<T, bool>> where = null, in Expression<Func<T, object>> selector = null, in string key = null, in bool refresh = false)
        {
            var calculatedHash = SQLGenerator<T>.CalculateExpressionKey(in where, in selector, null, typeof(T), SqlOperation.SelectList, _sqlValues.Provider, in key);
            var fromCache = _cache.TryGet(in calculatedHash, out QueryResult<List<T>> result);

            if (!fromCache || refresh)
                result = FetchList(in calculatedHash, in where, in selector);

            return result.Data;
        }

        private QueryResult<List<T>> FetchList<T>(in int calculatedKey, in Expression<Func<T, bool>> where, in Expression<Func<T, object>> selector)
        {
            var data = _database.Value.FetchList<T>(where, selector);
            var result = new QueryResult<List<T>>(MethodHandled.FetchListExpression, null, DateTime.UtcNow.Add(_ttl), in data, null, where, selector);
            _cache.AddOrUpdate(in calculatedKey, result);
            return result;
        }

        public List<T> FetchList<T>(in string script, in object parameters = null, in string key = null, in bool refresh = false)
        {
            int calculatedHash = CalculateHash(in script, in parameters, in key);
            var fromCache = _cache.TryGet(in calculatedHash, out QueryResult<List<T>> result);

            if (!fromCache || refresh)
            {
                var data = _database.Value.FetchList<T>(in script, in parameters);
                result = new QueryResult<List<T>>(MethodHandled.FetchListQueryString, in script, in parameters, in data, DateTime.UtcNow.Add(_ttl));
                _cache.AddOrUpdate(in calculatedHash, result);
            }

            return result.Data;
        }

        #endregion

        #region Tuple

        public Tuple<List<T1>, List<T2>> FetchTuple<T1, T2>(in string script, in object parameters = null, in string key = null, in bool refresh = false)
        {
            int calculatedHash = CalculateHash(in script, in parameters, in key);
            var fromCache = _cache.TryGet(in calculatedHash, out QueryResult<Tuple<List<T1>, List<T2>>> result);

            if (!fromCache || refresh)
            {
                var tuple = _database.Value.FetchTuple<T1, T2>(in script, in parameters);
                result = new QueryResult<Tuple<List<T1>, List<T2>>>(MethodHandled.FetchTupleQueryString_2, in script, in parameters, in tuple, DateTime.UtcNow.Add(_ttl));
                _cache.AddOrUpdate(in calculatedHash, result);
            }

            return result.Data;
        }

        public Tuple<List<T1>, List<T2>, List<T3>> FetchTuple<T1, T2, T3>(in string script, in object parameters = null, in string key = null, in bool refresh = false)
        {
            int calculatedHash = CalculateHash(in script, in parameters, in key);
            var fromCache = _cache.TryGet(in calculatedHash, out QueryResult<Tuple<List<T1>, List<T2>, List<T3>>> result);

            if (!fromCache || refresh)
            {
                var tuple = _database.Value.FetchTuple<T1, T2, T3>(in script, in parameters);
                result = new QueryResult<Tuple<List<T1>, List<T2>, List<T3>>>(MethodHandled.FetchTupleQueryString_3, in script, in parameters, in tuple, DateTime.UtcNow.Add(_ttl));
                _cache.AddOrUpdate(in calculatedHash, result);
            }

            return result.Data;
        }

        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>> FetchTuple<T1, T2, T3, T4>(in string script, in object parameters = null, in string key = null, in bool refresh = false)
        {
            int calculatedHash = CalculateHash(in script, in parameters, in key);
            var fromCache = _cache.TryGet(in calculatedHash, out QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> result);

            if (!fromCache || refresh)
            {
                var tuple = _database.Value.FetchTuple<T1, T2, T3, T4>(in script, in parameters);
                result = new QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>>(MethodHandled.FetchTupleQueryString_4, in script, in parameters, in tuple, DateTime.UtcNow.Add(_ttl));
                _cache.AddOrUpdate(in calculatedHash, result);
            }

            return result.Data;
        }

        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>> FetchTuple<T1, T2, T3, T4, T5>(in string script, in object parameters = null, in string key = null, in bool refresh = false)
        {
            int calculatedHash = CalculateHash(in script, in parameters, in key);
            var fromCache = _cache.TryGet(in calculatedHash, out QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>> result);

            if (!fromCache || refresh)
            {
                var tuple = _database.Value.FetchTuple<T1, T2, T3, T4, T5>(in script, in parameters);
                result = new QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>>(MethodHandled.FetchTupleQueryString_5, in script, in parameters, in tuple, DateTime.UtcNow.Add(_ttl));
                _cache.AddOrUpdate(in calculatedHash, result);
            }

            return result.Data;
        }

        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>> FetchTuple<T1, T2, T3, T4, T5, T6>(in string script, in object parameters = null, in string key = null, in bool refresh = false)
        {
            int calculatedHash = CalculateHash(in script, in parameters, in key);
            var fromCache = _cache.TryGet(calculatedHash, out QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>> result);

            if (!fromCache || refresh)
            {
                var tuple = _database.Value.FetchTuple<T1, T2, T3, T4, T5, T6>(in script, in parameters);
                result = new QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>>(MethodHandled.FetchTupleQueryString_6, in script, in parameters, in tuple, DateTime.UtcNow.Add(_ttl));
                _cache.AddOrUpdate(in calculatedHash, result);
            }

            return result.Data;
        }

        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>> FetchTuple<T1, T2, T3, T4, T5, T6, T7>(in string script, in object parameters = null, in string key = null, in bool refresh = false)
        {
            int calculatedHash = CalculateHash(in script, in parameters, in key);
            var fromCache = _cache.TryGet(in calculatedHash, out QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> result);

            if (!fromCache || refresh)
            {
                var tuple = _database.Value.FetchTuple<T1, T2, T3, T4, T5, T6, T7>(in script, in parameters);
                result = new QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>>(MethodHandled.FetchTupleQueryString_7, in script, in parameters, in tuple, DateTime.UtcNow.Add(_ttl));
                _cache.AddOrUpdate(in calculatedHash, result);
            }

            return result.Data;
        }

        #endregion

        #region Management

        public void Clear()
        {
            _cache.Clear();
        }

        public void Clear(in string key)
        {
            _cache.Clear(HashHelper.GenerateHash(key));
        }

        private readonly static Type CachedDatabaseType = typeof(CachedDatabase)!;

        public void Refresh(in string key, in bool throwErrorIfNotFound = false)
        {
            var calculatedHash = HashHelper.GenerateHash(key);
            if (_cache.TryGetValueForRefresh(calculatedHash, out IQueryResult item))
            {
                if (item == null && throwErrorIfNotFound)
                    throw new ArgumentNullException();

                Type genericType = item.GetType().GetGenericArguments()[0];
                string handler = item.MethodHandled.ToString();
                string methodName = handler.IndexOf("List") > 0 ? "FetchList" : handler.IndexOf("One") > 0 ? "FetchOne" : "FetchTuple";

                MethodInfo methodInfo = null;
                switch (item.MethodHandled)
                {
                    case MethodHandled.FetchListExpression:
                        methodInfo = CachedDatabaseType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
                        break;
                    case MethodHandled.FetchListQueryString:
                        methodInfo = CachedDatabaseType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(string), typeof(object), typeof(string), typeof(bool) }, null);
                        break;
                    case MethodHandled.FetchOneExpression:
                        methodInfo = CachedDatabaseType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
                        break;
                    case MethodHandled.FetchOneQueryString:
                        methodInfo = CachedDatabaseType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(string), typeof(object), typeof(string), typeof(bool) }, null);
                        break;
                    case MethodHandled.FetchTupleQueryString_2:
                        methodInfo = GetTupleMethod(2);
                        break;
                    case MethodHandled.FetchTupleQueryString_3:
                        methodInfo = GetTupleMethod(3);
                        break;
                    case MethodHandled.FetchTupleQueryString_4:
                        methodInfo = GetTupleMethod(4);
                        break;
                    case MethodHandled.FetchTupleQueryString_5:
                        methodInfo = GetTupleMethod(5);
                        break;
                    case MethodHandled.FetchTupleQueryString_6:
                        methodInfo = GetTupleMethod(6);
                        break;
                    case MethodHandled.FetchTupleQueryString_7:
                        methodInfo = GetTupleMethod(7);
                        break;
                    default:
                        break;
                }

                if (methodInfo == null)
                    throw new ArgumentNullException($"Method '{methodName}' not found in '{GetType().Name}'.");

                Type[] tupleArgs = methodName.Equals("FetchTuple") ? ReflectionHelper.GetTupleGenericArguments(genericType) : null;

                if (item.IsTuple && (tupleArgs == null || tupleArgs.Length == 0))
                    throw new MissingMemberException($"Tuple arguments not found for '{genericType.Name}'.");

                _ = item.MethodHandled switch
                {
                    MethodHandled.FetchListExpression => methodInfo.MakeGenericMethod(genericType.GenericTypeArguments[0]).Invoke(this, new object[] { calculatedHash, item.Where, item.Selector }),
                    MethodHandled.FetchListQueryString => methodInfo.MakeGenericMethod(genericType.GenericTypeArguments[0]).Invoke(this, new object[] { item.Query, item.Params, key, true }),
                    MethodHandled.FetchOneExpression => methodInfo.MakeGenericMethod(genericType).Invoke(this, new object[] { calculatedHash, item.Where, item.Selector }),
                    MethodHandled.FetchOneQueryString => methodInfo.MakeGenericMethod(genericType).Invoke(this, new object[] { item.Query, item.Params, key, true }),
                    MethodHandled.FetchTupleQueryString_2 => methodInfo.MakeGenericMethod(tupleArgs[0], tupleArgs[1]).Invoke(this, new object[] { item.Query, item.Params, key, true }),
                    MethodHandled.FetchTupleQueryString_3 => methodInfo.MakeGenericMethod(tupleArgs[0], tupleArgs[1], tupleArgs[2]).Invoke(this, new object[] { item.Query, item.Params, key, true }),
                    MethodHandled.FetchTupleQueryString_4 => methodInfo.MakeGenericMethod(tupleArgs[0], tupleArgs[1], tupleArgs[2], tupleArgs[3]).Invoke(this, new object[] { item.Query, item.Params, key, true }),
                    MethodHandled.FetchTupleQueryString_5 => methodInfo.MakeGenericMethod(tupleArgs[0], tupleArgs[1], tupleArgs[2], tupleArgs[3], tupleArgs[4]).Invoke(this, new object[] { item.Query, item.Params, key, true }),
                    MethodHandled.FetchTupleQueryString_6 => methodInfo.MakeGenericMethod(tupleArgs[0], tupleArgs[1], tupleArgs[2], tupleArgs[3], tupleArgs[4], tupleArgs[5]).Invoke(this, new object[] { item.Query, item.Params, key, true }),
                    MethodHandled.FetchTupleQueryString_7 => methodInfo.MakeGenericMethod(tupleArgs[0], tupleArgs[1], tupleArgs[2], tupleArgs[3], tupleArgs[4], tupleArgs[5], tupleArgs[6]).Invoke(this, new object[] { item.Query, item.Params, key, true }),
                    MethodHandled.Execute => throw new NotImplementedException(),
                    _ => throw new NotImplementedException(),
                };
            }
            else
            {
                if (throwErrorIfNotFound)
                    throw new KeyNotFoundException($"Item with key '{key}' not found in cache.");
            }
        }

        private static MethodInfo GetTupleMethod(int parameterCount)
        {
            var methods = CachedDatabaseType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                              .Where(m => m.Name == "FetchTuple");

            return methods?.FirstOrDefault(m => m.ReturnType.GenericTypeArguments.Length == parameterCount);
        }

        public bool TryGetStringValue(in string key, out string data)
        {
            if (_cache.IsMemoryCache)
            {
                throw new Exception("This method is not supported for memory cache.");
            }

            var calculatedHash = HashHelper.GenerateHash(key);

            return _cache.TryGetString(in calculatedHash, out data);
        }

        public bool TryGetBytesValue(in string key, out byte[] data)
        {
            if (_cache.IsMemoryCache)
            {
                throw new Exception("This method is not supported for memory cache.");
            }

            var calculatedHash = HashHelper.GenerateHash(key);
            return _cache.TryGetBytes(in calculatedHash, out data);
        }

        public bool CanLoadStream(in string key)
        {
            if (_cache.IsMemoryCache)
            {
                throw new Exception("This method is not supported for memory cache.");
            }

            var calculatedHash = HashHelper.GenerateHash(key);
            return _cache.CanLoadStream(in calculatedHash);
        }

        public void LoadStream(in string key, in StreamWriter stream)
        {
            if (_cache.IsMemoryCache)
            {
                throw new Exception("This method is not supported for memory cache.");
            }

            var calculatedHash = HashHelper.GenerateHash(key);
            _cache.LoadStream(in calculatedHash, in stream);
        }

        public async Task LoadStreamAsync(string key, StreamWriter stream)
        {
            if (_cache.IsMemoryCache)
            {
                throw new Exception("This method is not supported for memory cache.");
            }

            var calculatedHash = HashHelper.GenerateHash(key);
            await _cache.LoadStreamAsync(calculatedHash, stream);
        }

        #endregion

    }
}
