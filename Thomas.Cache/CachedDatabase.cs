using System;
using System.Collections.Generic;
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
        void Clear();
        void Clear(string key);
        void Refresh(string key, bool throwErrorIfNotFound = false);
    }

    internal sealed class CachedDatabase : ICachedDatabase
    {
        private readonly IDbDataCache _cache;
        private readonly IDatabase _database;
        private readonly ISqlFormatter _sqlValues;

        internal CachedDatabase(IDbDataCache cache, IDatabase database, ISqlFormatter sqlValues)
        {
            _cache = cache;
            _database = database;
            _sqlValues = sqlValues;
        }

        #region Single

        public T? ToSingle<T>(string script, object? parameters = null, bool refresh = false, string? identifier = null) where T : class, new()
        {
            var k = !string.IsNullOrEmpty(identifier) ? HashHelper.GenerateHash(identifier) : HashHelper.GenerateHash(script, parameters);

            var fromCache = _cache.TryGet(k, out QueryResult<T>? result);

            if (!fromCache || refresh)
            {
                var data = _database.ToSingle<T>(script, parameters);
                result = new QueryResult<T>(MethodHandled.ToSingleQueryString, script, parameters, data);
                _cache.AddOrUpdate(k, result);
            }

            return result.Data;
        }

        public T? ToSingle<T>(Expression<Func<T, bool>>? where = null, bool refresh = false, string? key = null) where T : class, new()
        {
            var calculatedKey = SqlGenerator<T>.CalculateExpressionKey<T>(where, SqlOperation.SelectSingle, _sqlValues.Provider, key);
            var fromCache = _cache.TryGet(calculatedKey, out QueryResult<T> result);

            T data = result?.Data;

            if (!fromCache || refresh)
                data = ToSingle(calculatedKey, where);

            return data;
        }

        private T ToSingle<T>(ulong calculatedKey, Expression<Func<T, bool>>? where = null) where T : class, new()
        {
            var data = _database.ToSingle<T>(where);
            var result = new QueryResult<T>(MethodHandled.ToSingleExpression, null, null, data, where);
            _cache.AddOrUpdate(calculatedKey, result);
            return result.Data;
        }

        #endregion

        #region List

        public List<T> ToList<T>(Expression<Func<T, bool>>? where = null, bool refresh = false, string? key = null) where T : class, new()
        {
            var calculatedKey = SqlGenerator<T>.CalculateExpressionKey<T>(where, SqlOperation.SelectList, _sqlValues.Provider, key);
            var fromCache = _cache.TryGet(calculatedKey, out QueryResult<List<T>> result);
            List<T> data = result?.Data;

            if (!fromCache || refresh)
                data = ToList(calculatedKey, where);

            return data;
        }

        private List<T> ToList<T>(ulong calculatedKey, Expression<Func<T, bool>>? where = null) where T : class, new()
        {
            var data = _database.ToList<T>(where);
            var result = new QueryResult<List<T>>(MethodHandled.ToListExpression, null, null, data, where);
            _cache.AddOrUpdate(calculatedKey, result);
            return result.Data;
        }

        public List<T> ToList<T>(string script, object? parameters, bool refresh = false, string? key = null) where T : class, new()
        {
            var k = !string.IsNullOrEmpty(key) ? HashHelper.GenerateHash(key) : HashHelper.GenerateHash(script, parameters);

            var fromCache = _cache.TryGet(k, out QueryResult<List<T>>? result);

            if (!fromCache || refresh)
            {
                var data = _database.ToList<T>(script, parameters);
                result = new QueryResult<List<T>>(MethodHandled.ToListQueryString, script, parameters, data);
                _cache.AddOrUpdate(k, result);
            }

            return result.Data;
        }

        //public List<T> ToList<T, TFilter>(string script, ref TFilter parameters, bool refresh = false, string? key = null) where T : class, new()
        //{
        //    var k = !string.IsNullOrEmpty(key) ? HashHelper.GenerateHash(key) : HashHelper.GenerateHash(script, parameters);

        //    var fromCache = _cache.TryGet(k, out QueryResult<List<T>>? result);

        //    if (!fromCache || refresh)
        //    {
        //        var data = _database.ToList<T>(script, parameters);
        //        result = new QueryResult<List<T>>(MethodHandled.ToListQueryString, script, parameters, data);
        //        _cache.AddOrUpdate(k, result);
        //    }
        //    else
        //    {
        //        parameters = (TFilter)result.Params;
        //    }

        //    return result.Data;
        //}
        #endregion

        #region Tuple

        public Tuple<List<T1>, List<T2>> ToTuple<T1, T2>(string script, object? parameters = null, bool refresh = false, string? key = null)
            where T1 : class, new()
            where T2 : class, new()
        {

            var k = !string.IsNullOrEmpty(key) ? HashHelper.GenerateHash(key) : HashHelper.GenerateHash(script, parameters);

            var fromCache = _cache.TryGet(k, out QueryResult<Tuple<List<T1>, List<T2>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2>(script, parameters);
                result = new QueryResult<Tuple<List<T1>, List<T2>>>(MethodHandled.ToTupleQueryString_2, script, parameters, tuple);
                _cache.AddOrUpdate(k, result);
            }

            return result.Data;
        }

        public Tuple<List<T1>, List<T2>, List<T3>> ToTuple<T1, T2, T3>(string script, object? parameters = null, bool refresh = false, string? key = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
        {
            var k = !string.IsNullOrEmpty(key) ? HashHelper.GenerateHash(key) : HashHelper.GenerateHash(script, parameters);
            var fromCache = _cache.TryGet(k, out QueryResult<Tuple<List<T1>, List<T2>, List<T3>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2, T3>(script, parameters);
                result = new QueryResult<Tuple<List<T1>, List<T2>, List<T3>>>(MethodHandled.ToTupleQueryString_3, script, parameters, tuple);
                _cache.AddOrUpdate(k, result);
            }

            return result.Data;
        }

        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>> ToTuple<T1, T2, T3, T4>(string script, object? parameters = null, bool refresh = false, string? key = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
        {
            var k = !string.IsNullOrEmpty(key) ? HashHelper.GenerateHash(key) : HashHelper.GenerateHash(script, parameters);

            var fromCache = _cache.TryGet(k, out QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2, T3, T4>(script, parameters);
                result = new QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>>(MethodHandled.ToTupleQueryString_4, script, parameters, tuple);
                _cache.AddOrUpdate(k, result);
            }

            return result.Data;
        }

        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>> ToTuple<T1, T2, T3, T4, T5>(string script, object? parameters = null, bool refresh = false, string? key = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
        {
            var k = !string.IsNullOrEmpty(key) ? HashHelper.GenerateHash(key) : HashHelper.GenerateHash(script, parameters);

            var fromCache = _cache.TryGet(k, out QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2, T3, T4, T5>(script, parameters);
                result = new QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>>(MethodHandled.ToTupleQueryString_5, script, parameters, tuple);
                _cache.AddOrUpdate(k, result);
            }

            return result.Data;
        }

        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>> ToTuple<T1, T2, T3, T4, T5, T6>(string script, object? parameters = null, bool refresh = false, string? key = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
        {
            var k = !string.IsNullOrEmpty(key) ? HashHelper.GenerateHash(key) : HashHelper.GenerateHash(script, parameters);

            var fromCache = _cache.TryGet(k, out QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2, T3, T4, T5, T6>(script, parameters);
                result = new QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>>(MethodHandled.ToTupleQueryString_6, script, parameters, tuple);
                _cache.AddOrUpdate(k, result);
            }

            return result.Data;
        }

        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>> ToTuple<T1, T2, T3, T4, T5, T6, T7>(string script, object? parameters = null, bool refresh = false, string? key = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
            where T7 : class, new()
        {
            var k = !string.IsNullOrEmpty(key) ? HashHelper.GenerateHash(key) : HashHelper.GenerateHash(script, parameters);

            var fromCache = _cache.TryGet(k, out QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2, T3, T4, T5, T6, T7>(script, parameters);
                result = new QueryResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>>(MethodHandled.ToTupleQueryString_7, script, parameters, tuple);
                _cache.AddOrUpdate(k, result);
            }

            return result.Data;
        }

        #endregion

        #region management

        public void Clear()
        {
            _cache.Clear();
        }

        public void Clear(string key)
        {
            var ulongKey = HashHelper.GenerateHash(key);
            _cache.Clear(ulongKey);
        }

        public void Refresh(string key, bool throwErrorIfNotFound = false)
        {
            var calculatedKey = HashHelper.GenerateHash(key);
            if (DbDataCache.TryGetValue(calculatedKey, out IQueryResult item))
            {
                Type genericType = item.GetType().GetGenericArguments()[0];
                string handler = item.MethodHandled.ToString();
                string methodName = handler.IndexOf("List") > 0 ? "ToList" : handler.IndexOf("Single") > 0 ? "ToSingle" : "ToTuple";

                MethodInfo methodInfo = null;
                switch (item.MethodHandled)
                {
                    case MethodHandled.ToListExpression:
                        methodInfo = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
                        break;
                    case MethodHandled.ToListQueryString:
                        methodInfo = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public, new Type[] { typeof(string), typeof(object), typeof(bool), typeof(string) });
                        break;
                    case MethodHandled.ToSingleExpression:
                        methodInfo = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
                        break;
                    case MethodHandled.ToSingleQueryString:
                        methodInfo = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public, new Type[] { typeof(string), typeof(object), typeof(bool), typeof(string) });
                        break;
                    case MethodHandled.ToTupleQueryString_2:
                    case MethodHandled.ToTupleQueryString_3:
                    case MethodHandled.ToTupleQueryString_4:
                    case MethodHandled.ToTupleQueryString_5:
                    case MethodHandled.ToTupleQueryString_6:
                    case MethodHandled.ToTupleQueryString_7:
                        methodInfo = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public, new Type[] { typeof(string), typeof(object), typeof(bool), typeof(string) });
                        break;
                    default:
                        break;
                }

                var tupleArgs = methodName.Equals("ToTuple") ? ReflectionHelper.GetTupleGenericArguments(genericType) : null;

                var _ = item.MethodHandled switch
                {
                    MethodHandled.ToListExpression => methodInfo.MakeGenericMethod(genericType.GenericTypeArguments[0]).Invoke(this, new object[] { calculatedKey, item.Where }),
                    MethodHandled.ToListQueryString => methodInfo.MakeGenericMethod(genericType.GenericTypeArguments[0]).Invoke(this, new object[] { item.Query, item.Params, true, key }),
                    MethodHandled.ToSingleExpression => methodInfo.MakeGenericMethod(genericType).Invoke(this, new object[] { calculatedKey, item.Where }),
                    MethodHandled.ToSingleQueryString => methodInfo.MakeGenericMethod(genericType).Invoke(this, new object[] { item.Query, item.Params, true, key }),
                    MethodHandled.ToTupleQueryString_2 => methodInfo.MakeGenericMethod(tupleArgs[0], tupleArgs[1]).Invoke(this, new object[] { item.Query, item.Params, true, key }),
                    MethodHandled.ToTupleQueryString_3 => methodInfo.MakeGenericMethod(tupleArgs[0], tupleArgs[1], tupleArgs[2]).Invoke(this, new object[] { item.Query, item.Params, true, key }),
                    MethodHandled.ToTupleQueryString_4 => methodInfo.MakeGenericMethod(tupleArgs[0], tupleArgs[1], tupleArgs[2], tupleArgs[3]).Invoke(this, new object[] { item.Query, item.Params, true, key }),
                    MethodHandled.ToTupleQueryString_5 => methodInfo.MakeGenericMethod(tupleArgs[0], tupleArgs[1], tupleArgs[2], tupleArgs[3], tupleArgs[4]).Invoke(this, new object[] { item.Query, item.Params, true, key }),
                    MethodHandled.ToTupleQueryString_6 => methodInfo.MakeGenericMethod(tupleArgs[0], tupleArgs[1], tupleArgs[2], tupleArgs[3], tupleArgs[4], tupleArgs[5]).Invoke(this, new object[] { item.Query, item.Params, true, key }),
                    MethodHandled.ToTupleQueryString_7 => methodInfo.MakeGenericMethod(tupleArgs[0], tupleArgs[1], tupleArgs[2], tupleArgs[3], tupleArgs[4], tupleArgs[5], tupleArgs[6]).Invoke(this, new object[] { item.Query, item.Params, true, key }),
                };
            }
            else
            {
                if (throwErrorIfNotFound)
                    throw new KeyNotFoundException($"Item with key '{key}' not found in cache.");
            }
        }

        #endregion


    }
}
