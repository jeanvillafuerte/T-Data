using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Thomas.Cache.Helpers;
using Thomas.Cache.MemoryCache;
using Thomas.Database;
using Thomas.Database.Core.Converters;
using Thomas.Database.Core.QueryGenerator;
using Thomas.Database.Helpers;

namespace Thomas.Cache
{
    public interface ICachedDatabase : IDbResulCachedSet
    {
        void Release();
        void Release(string key);
        void Refresh(string key, bool throwErrorIfNotFound = false);
    }

    internal sealed class CachedDatabase : ICachedDatabase
    {
        private readonly IDbDataCache _cache;
        private readonly IDatabase _database;
        private CultureInfo _cultureInfo;
        private readonly ISqlFormatter _sqlValues;
        public DbDataConverter _dbDataConverter;

        internal CachedDatabase(IDbDataCache cache, IDatabase database, in CultureInfo cultureInfo, ISqlFormatter sqlValues)
        {
            _cache = cache;
            _database = database;
            _cultureInfo = cultureInfo;
            _sqlValues = sqlValues;
            _dbDataConverter = new DbDataConverter(_sqlValues.Provider);
        }

        private static bool CheckIfNotAnonymousType(Type type)
        {
            return !(Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                && type.IsGenericType && type.Name.Contains("AnonymousType")
                && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                && type.Attributes.HasFlag(TypeAttributes.NotPublic));
        }

        private void ReadParameter(string script, object? parameters = null, bool fromCache = false)
        {
            if (parameters != null)
            {
                if (CheckIfNotAnonymousType(parameters.GetType()))
                {
                    string inputIdentifies = HashHelper.GenerateHash($"cache_params_{script}", parameters);

                    if (fromCache)
                    {
                        DbParameterCache.Instance.TryGet(inputIdentifies, out var output);

                        var dataParameters = _database.GetMetadataParameter(script, parameters);

                        foreach (var item in dataParameters.Where(x => x.IsOutParameter))
                        {
                            var value = item.GetValue(ref output);
                            item.SetValue(ref parameters, ref value, ref _cultureInfo);
                        }
                    }
                    else
                        DbParameterCache.Instance.AddOrUpdate(inputIdentifies, parameters);
                }
            }
        }

        #region Single

        public T? ToSingle<T>(string script, object? parameters = null, bool refresh = false, string? key = null) where T : class, new()
        {
            var k = !string.IsNullOrEmpty(key) ? key : HashHelper.GenerateHash(script, parameters);

            var fromCache = _cache.TryGet(k, out QueryResult<T>? result);


            if (!fromCache || refresh)
            {
                var item = _database.ToSingle<T>(script, parameters);
                result = new QueryResult<T>(MethodHandled.ToSingleQueryString, script, parameters, item);
                _cache.AddOrUpdate(k, result);
            }

            ReadParameter(script, parameters, fromCache & !refresh);

            return result.Data;
        }

        public T? ToSingle<T>(Expression<Func<T, bool>>? where = null, bool refresh = false, string? key = null) where T : class, new()
        {
            var generator = new SqlGenerator<T>(_sqlValues, _cultureInfo, _dbDataConverter.Converters);
            var script = generator.GenerateSelectWhere(where);
            var k = !string.IsNullOrEmpty(key) ? key : HashHelper.GenerateHash(script, null);

            var fromCache = _cache.TryGet(k, out QueryResult<T>? result);

            if (!fromCache || refresh)
                result = ToSingle<T>(script, generator.DbParametersToBind, k);

            return result.Data;
        }

        private QueryResult<T> ToSingle<T>(string script, Dictionary<string, QueryParameter> parameters, string key) where T : class, new()
        {
            var data = (_database as DatabaseBase).ToSingle<T>(script, parameters);
            var result = new QueryResult<T>(MethodHandled.ToSingleExpression, script, parameters, data);
            _cache.AddOrUpdate(key, result);
            return result;
        }

        #endregion

        #region List

        public IEnumerable<T> ToList<T>(Expression<Func<T, bool>>? where = null, bool refresh = false, string? key = null) where T : class, new()
        {
            var k = !string.IsNullOrEmpty(key) ? key : ExpressionHasher.GetHashCode(where, _sqlValues.Provider).ToString();
            var fromCache = _cache.TryGet(k, out QueryResult<IEnumerable<T>> result);
            IEnumerable<T> data = result?.Data;

            if (!fromCache || refresh)
                data = ToList(where, k);

            return data;
        }

        private IEnumerable<T> ToList<T>(Expression<Func<T, bool>>? where = null, string? key = null) where T : class, new()
        {
            var data = _database.ToList<T>(where);
            var result = new QueryResult<IEnumerable<T>>(MethodHandled.ToListExpression, null, null, data, where);
            _cache.AddOrUpdate(key, result);
            return result.Data;
        }

        public IEnumerable<T> ToList<T>(string script, object? parameters = null, bool refresh = false, string? key = null) where T : class, new()
        {
            var k = !string.IsNullOrEmpty(key) ? key : HashHelper.GenerateHash(script, parameters);

            var fromCache = _cache.TryGet(k, out QueryResult<IEnumerable<T>>? result);

            if (!fromCache || refresh)
            {
                var data = _database.ToList<T>(script, parameters);
                result = new QueryResult<IEnumerable<T>>(MethodHandled.ToListQueryString, script, parameters, data);
                _cache.AddOrUpdate(k, result);
            }

            ReadParameter(script, parameters, fromCache & !refresh);

            return result.Data;
        }

        #endregion

        #region Tuple

        public Tuple<IEnumerable<T1>, IEnumerable<T2>> ToTuple<T1, T2>(string script, object? parameters = null, bool refresh = false, string? key = null)
            where T1 : class, new()
            where T2 : class, new()
        {

            var k = !string.IsNullOrEmpty(key) ? key : HashHelper.GenerateHash(script, parameters);

            var fromCache = _cache.TryGet(k, out QueryResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2>(script, parameters);
                result = new QueryResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>>(MethodHandled.ToTupleQueryString_2, script, parameters, tuple);
                _cache.AddOrUpdate(k, result);
            }

            ReadParameter(script, parameters, fromCache & !refresh);

            return result.Data;
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>> ToTuple<T1, T2, T3>(string script, object? parameters = null, bool refresh = false, string? key = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
        {
            var k = !string.IsNullOrEmpty(key) ? key : HashHelper.GenerateHash(script, parameters);
            var fromCache = _cache.TryGet(k, out QueryResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2, T3>(script, parameters);
                result = new QueryResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>(MethodHandled.ToTupleQueryString_3, script, parameters, tuple);
                _cache.AddOrUpdate(k, result);
            }

            ReadParameter(script, parameters, fromCache & !refresh);

            return result.Data;
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>> ToTuple<T1, T2, T3, T4>(string script, object? parameters = null, bool refresh = false, string? key = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
        {
            var k = !string.IsNullOrEmpty(key) ? key : HashHelper.GenerateHash(script, parameters);

            var fromCache = _cache.TryGet(k, out QueryResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2, T3, T4>(script, parameters);
                result = new QueryResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>(MethodHandled.ToTupleQueryString_4, script, parameters, tuple);
                _cache.AddOrUpdate(k, result);
            }

            ReadParameter(script, parameters, fromCache & !refresh);

            return result.Data;
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>> ToTuple<T1, T2, T3, T4, T5>(string script, object? parameters = null, bool refresh = false, string? key = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
        {
            var k = !string.IsNullOrEmpty(key) ? key : HashHelper.GenerateHash(script, parameters);

            var fromCache = _cache.TryGet(k, out QueryResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2, T3, T4, T5>(script, parameters);
                result = new QueryResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>(MethodHandled.ToTupleQueryString_5, script, parameters, tuple);
                _cache.AddOrUpdate(k, result);
            }

            ReadParameter(script, parameters, fromCache & !refresh);

            return result.Data;
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>> ToTuple<T1, T2, T3, T4, T5, T6>(string script, object? parameters = null, bool refresh = false, string? key = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
        {
            var k = !string.IsNullOrEmpty(key) ? key : HashHelper.GenerateHash(script, parameters);

            var fromCache = _cache.TryGet(k, out QueryResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2, T3, T4, T5, T6>(script, parameters);
                result = new QueryResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>(MethodHandled.ToTupleQueryString_6, script, parameters, tuple);
                _cache.AddOrUpdate(k, result);
            }

            ReadParameter(script, parameters, fromCache & !refresh);

            return result.Data;
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>> ToTuple<T1, T2, T3, T4, T5, T6, T7>(string script, object? parameters = null, bool refresh = false, string? key = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
            where T7 : class, new()
        {
            var k = !string.IsNullOrEmpty(key) ? key : HashHelper.GenerateHash(script, parameters);

            var fromCache = _cache.TryGet(k, out QueryResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2, T3, T4, T5, T6, T7>(script, parameters);
                result = new QueryResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>(MethodHandled.ToTupleQueryString_7, script, parameters, tuple);
                _cache.AddOrUpdate(k, result);
            }

            ReadParameter(script, parameters, fromCache & !refresh);

            return result.Data;
        }

        #endregion

        #region management

        public void Release()
        {
            _cache.Release();
            DbParameterCache.Instance.Release();
        }

        public void Release(string key)
        {
            _cache.Release(key);
            DbParameterCache.Instance.Release(key);
        }

        public void Refresh(string key, bool throwErrorIfNotFound = false)
        {
            if (DbDataCache.Instance.TryGetValue(key, out IQueryResult item))
            {
                Type genericType = item.GetType().GetGenericArguments()[0];
                string handler = item.MethodHandled.ToString();
                string methodName = handler.IndexOf("List") > 0 ? "ToList": handler.IndexOf("Single") > 0 ? "ToSingle" : "ToTuple";
                bool isPublic = item.MethodHandled.ToString().Contains("QueryString");

                MethodInfo m = GetType().GetMethod(methodName, isPublic ? BindingFlags.Public : BindingFlags.Instance | BindingFlags.NonPublic);
                var tupleArgs = methodName.Equals("ToTuple") ? ReflectionHelper.GetTupleGenericArguments(genericType) : null;

                var _ = item.MethodHandled switch
                {
                    MethodHandled.ToListExpression => m.MakeGenericMethod(ReflectionHelper.GetIEnumerableElementType(genericType)).Invoke(this, new object[] { item.Expression, key }),
                    MethodHandled.ToListQueryString => m.MakeGenericMethod(ReflectionHelper.GetIEnumerableElementType(genericType)).Invoke(this, new object[] { item.Query, item.Params, true, key }),
                    MethodHandled.ToSingleExpression => m.MakeGenericMethod(genericType).Invoke(this, new object[] { item.Query, item.Params, key }),
                    MethodHandled.ToSingleQueryString => m.MakeGenericMethod(genericType).Invoke(this, new object[] { item.Query, item.Params, true, key }),
                    MethodHandled.ToTupleQueryString_2 => m.MakeGenericMethod(tupleArgs[0], tupleArgs[1]).Invoke(this, new object[] { item.Query, item.Params, true, key }),
                    MethodHandled.ToTupleQueryString_3 => m.MakeGenericMethod(tupleArgs[0], tupleArgs[1], tupleArgs[2]).Invoke(this, new object[] { item.Query, item.Params, true, key }),
                    MethodHandled.ToTupleQueryString_4 => m.MakeGenericMethod(tupleArgs[0], tupleArgs[1], tupleArgs[2], tupleArgs[3]).Invoke(this, new object[] { item.Query, item.Params, true, key }),
                    MethodHandled.ToTupleQueryString_5 => m.MakeGenericMethod(tupleArgs[0], tupleArgs[1], tupleArgs[2], tupleArgs[3], tupleArgs[4]).Invoke(this, new object[] { item.Query, item.Params, true, key }),
                    MethodHandled.ToTupleQueryString_6 => m.MakeGenericMethod(tupleArgs[0], tupleArgs[1], tupleArgs[2], tupleArgs[3], tupleArgs[4], tupleArgs[5]).Invoke(this, new object[] { item.Query, item.Params, true, key }),
                    MethodHandled.ToTupleQueryString_7 => m.MakeGenericMethod(tupleArgs[0], tupleArgs[1], tupleArgs[2], tupleArgs[3], tupleArgs[4], tupleArgs[5], tupleArgs[6]).Invoke(this, new object[] { item.Query, item.Params, true, key }),
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
