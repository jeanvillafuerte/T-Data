using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Thomas.Cache.Helpers;
using Thomas.Cache.MemoryCache;
using Thomas.Database;
using Thomas.Database.Core.QueryGenerator;

namespace Thomas.Cache
{
    public interface ICachedDatabase : IDbResulCachedSet
    {
        void Clear(string key);
        void Refresh(string key, bool throwErrorIfNotFound = false);
    }

    internal sealed class CachedDatabase : ICachedDatabase
    {
        private readonly IDbDataCache _cache;
        private readonly Lazy<IDatabase> _database;
        private readonly ISqlFormatter _sqlValues;

        internal CachedDatabase(IDbDataCache cache, Lazy<IDatabase> database, ISqlFormatter sqlValues)
        {
            _cache = cache;
            _database = database;
            _sqlValues = sqlValues;
        }

        private int CalculateHash(string script, object? parameters, string? key)
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

        public T FetchOne<T>(string script, object? parameters = null, string? key = null, bool refresh = false)
        {
            int calculatedKey = CalculateHash(script, parameters, key);
            var fromCache = _cache.TryGet(calculatedKey, out QueryResult<T>? result);

            if (!fromCache || refresh)
            {
                var data = _database.Value.FetchOne<T>(script, parameters);
                result = new QueryResult<T>(MethodHandled.FetchOneQueryString, script, parameters, data);
                _cache.AddOrUpdate(calculatedKey, result);
            }

            return result.Data;
        }


        public T FetchOne<T>(Expression<Func<T, bool>> where = null, string? key = null, bool refresh = false)
        {
            var calculatedKey = SQLGenerator<T>.CalculateExpressionKey(where, null, typeof(T), SqlOperation.SelectSingle, _sqlValues.Provider, in key);
            var fromCache = _cache.TryGet(calculatedKey, out QueryResult<T>? result);

            if (!fromCache || refresh)
                result = FetchOne(calculatedKey, where);

            return result.Data;
        }

        private QueryResult<T> FetchOne<T>(int calculatedKey, Expression<Func<T, bool>> where)
        {
            var data = _database.Value.FetchOne<T>(where);
            var result = new QueryResult<T>(MethodHandled.FetchOneExpression, null, null, data, where);
            _cache.AddOrUpdate(calculatedKey, result);
            return result;
        }

        #endregion

        #region List

        public List<T> FetchList<T>(Expression<Func<T, bool>>? where = null, string? key = null, bool refresh = false)
        {
            var calculatedHash = SQLGenerator<T>.CalculateExpressionKey(where, null, typeof(T), SqlOperation.SelectList, _sqlValues.Provider, in key);
            var fromCache = _cache.TryGet(calculatedHash, out QueryResult<List<T>>? result);

            if (!fromCache || refresh)
                result = FetchList(calculatedHash, where);

            return result.Data;
        }

        private QueryResult<List<T>> FetchList<T>(int calculatedKey, Expression<Func<T, bool>> where)
        {
            var data = _database.Value.FetchList<T>(where);
            var result = new QueryResult<List<T>>(MethodHandled.FetchListExpression, null, null, data, where);
            _cache.AddOrUpdate(calculatedKey, result);
            return result;
        }

        public List<T> FetchList<T>(string script, object? parameters, string? key = null, bool refresh = false)
        {
            int calculatedHash = CalculateHash(script, parameters, key);
            var fromCache = _cache.TryGet(calculatedHash, out QueryResult<List<T>>? result);

            if (!fromCache || refresh)
            {
                var data = _database.Value.FetchList<T>(script, parameters);
                result = new QueryResult<List<T>>(MethodHandled.FetchListQueryString, script, parameters, data);
                _cache.AddOrUpdate(calculatedHash, result);
            }

            return result.Data;
        }

        #endregion

        #region Tuple

        public Tuple<List<T1>, List<T2>> FetchTuple<T1, T2>(string script, object? parameters = null, string? key = null, bool refresh = false)
        {

            int calculatedHash = CalculateHash(script, parameters, key);
            var fromCache = _cache.TryGet(calculatedHash, out QueryResult<Tuple<List<T1>, List<T2>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.Value.FetchTuple<T1, T2>(script, parameters);
                result = new QueryResult<Tuple<List<T1>, List<T2>>>(MethodHandled.FetchTupleQueryString_2, script, parameters, tuple);
                _cache.AddOrUpdate(calculatedHash, result);
            }

            return result.Data;
        }

        public Tuple<List<T1>, List<T2>, List<T3>> FetchTuple<T1, T2, T3>(string script, object? parameters = null, string? key = null, bool refresh = false)
        {
            int calculatedHash = CalculateHash(script, parameters, key);
            var fromCache = _cache.TryGet(calculatedHash, out QueryResult<Tuple<List<T1>, List<T2>, List<T3>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.Value.FetchTuple<T1, T2, T3>(script, parameters);
                result = new QueryResult<Tuple<List<T1>, List<T2>, List<T3>>>(MethodHandled.FetchTupleQueryString_3, script, parameters, tuple);
                _cache.AddOrUpdate(calculatedHash, result);
            }

            return result.Data;
        }

        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>> FetchTuple<T1, T2, T3, T4>(string script, object? parameters = null, string? key = null, bool refresh = false)
        {
            int calculatedHash = CalculateHash(script, parameters, key);
            var fromCache = _cache.TryGet(calculatedHash, out QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.Value.FetchTuple<T1, T2, T3, T4>(script, parameters);
                result = new QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>>(MethodHandled.FetchTupleQueryString_4, script, parameters, tuple);
                _cache.AddOrUpdate(calculatedHash, result);
            }

            return result.Data;
        }

        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>> FetchTuple<T1, T2, T3, T4, T5>(string script, object? parameters = null, string? key = null, bool refresh = false)
        {
            int calculatedHash = CalculateHash(script, parameters, key);
            var fromCache = _cache.TryGet(calculatedHash, out QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.Value.FetchTuple<T1, T2, T3, T4, T5>(script, parameters);
                result = new QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>>(MethodHandled.FetchTupleQueryString_5, script, parameters, tuple);
                _cache.AddOrUpdate(calculatedHash, result);
            }

            return result.Data;
        }

        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>> FetchTuple<T1, T2, T3, T4, T5, T6>(string script, object? parameters = null, string? key = null, bool refresh = false)
        {
            int calculatedHash = CalculateHash(script, parameters, key);
            var fromCache = _cache.TryGet(calculatedHash, out QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.Value.FetchTuple<T1, T2, T3, T4, T5, T6>(script, parameters);
                result = new QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>>(MethodHandled.FetchTupleQueryString_6, script, parameters, tuple);
                _cache.AddOrUpdate(calculatedHash, result);
            }

            return result.Data;
        }

        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>> FetchTuple<T1, T2, T3, T4, T5, T6, T7>(string script, object? parameters = null, string? key = null, bool refresh = false)
        {
            int calculatedHash = CalculateHash(script, parameters, key);
            var fromCache = _cache.TryGet(calculatedHash, out QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.Value.FetchTuple<T1, T2, T3, T4, T5, T6, T7>(script, parameters);
                result = new QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>>(MethodHandled.FetchTupleQueryString_7, script, parameters, tuple);
                _cache.AddOrUpdate(calculatedHash, result);
            }

            return result.Data;
        }

        #endregion

        #region management

        public void Clear(string key)
        {
            _cache.Clear(HashHelper.GenerateHash(key));
        }

        private readonly static Type CachedDatabaseType = typeof(CachedDatabase)!;

        public void Refresh(string key, bool throwErrorIfNotFound = false)
        {
            var calculatedHash = HashHelper.GenerateHash(key);
            if (DbDataCache.TryGetValue(calculatedHash, out IQueryResult? item))
            {
                if (item == null && throwErrorIfNotFound)
                    throw new ArgumentNullException();

                Type genericType = item!.GetType().GetGenericArguments()[0];
                string handler = item.MethodHandled.ToString();
                string methodName = handler.IndexOf("List") > 0 ? "FetchList" : handler.IndexOf("One") > 0 ? "FetchOne" : "FetchTuple";

                MethodInfo? methodInfo = null;
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

                Type[]? tupleArgs = methodName.Equals("FetchTuple") ? ReflectionHelper.GetTupleGenericArguments(genericType) : null;

                if (item.IsTuple && (tupleArgs == null || tupleArgs.Length == 0))
                    throw new MissingMemberException($"Tuple arguments not found for '{genericType.Name}'.");

                _ = item.MethodHandled switch
                {
                    MethodHandled.FetchListExpression => methodInfo.MakeGenericMethod(genericType.GenericTypeArguments[0]).Invoke(this, new object?[] { calculatedHash, item.Where }),
                    MethodHandled.FetchListQueryString => methodInfo.MakeGenericMethod(genericType.GenericTypeArguments[0]).Invoke(this, new object?[] { item.Query, item.Params, key, true }),
                    MethodHandled.FetchOneExpression => methodInfo.MakeGenericMethod(genericType).Invoke(this, new object?[] { calculatedHash, item.Where }),
                    MethodHandled.FetchOneQueryString => methodInfo.MakeGenericMethod(genericType).Invoke(this, new object?[] { item.Query, item.Params, key, true }),
                    MethodHandled.FetchTupleQueryString_2 => methodInfo.MakeGenericMethod(tupleArgs[0], tupleArgs[1]).Invoke(this, new object?[] { item.Query, item.Params, key, true }),
                    MethodHandled.FetchTupleQueryString_3 => methodInfo.MakeGenericMethod(tupleArgs[0], tupleArgs[1], tupleArgs[2]).Invoke(this, new object?[] { item.Query, item.Params, key, true }),
                    MethodHandled.FetchTupleQueryString_4 => methodInfo.MakeGenericMethod(tupleArgs[0], tupleArgs[1], tupleArgs[2], tupleArgs[3]).Invoke(this, new object?[] { item.Query, item.Params, key, true }),
                    MethodHandled.FetchTupleQueryString_5 => methodInfo.MakeGenericMethod(tupleArgs[0], tupleArgs[1], tupleArgs[2], tupleArgs[3], tupleArgs[4]).Invoke(this, new object?[] { item.Query, item.Params, key, true }),
                    MethodHandled.FetchTupleQueryString_6 => methodInfo.MakeGenericMethod(tupleArgs[0], tupleArgs[1], tupleArgs[2], tupleArgs[3], tupleArgs[4], tupleArgs[5]).Invoke(this, new object?[] { item.Query, item.Params, key, true }),
                    MethodHandled.FetchTupleQueryString_7 => methodInfo.MakeGenericMethod(tupleArgs[0], tupleArgs[1], tupleArgs[2], tupleArgs[3], tupleArgs[4], tupleArgs[5], tupleArgs[6]).Invoke(this, new object?[] { item.Query, item.Params, key, true }),
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

        private MethodInfo? GetTupleMethod(int parameterCount)
        {
            var methods = CachedDatabaseType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                              .Where(m => m.Name == "FetchTuple");

            return methods?.FirstOrDefault(m => m.ReturnType.GenericTypeArguments.Length == parameterCount);
        }
#endregion


    }
}
