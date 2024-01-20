using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Thomas.Cache.Helpers;
using Thomas.Cache.MemoryCache;
using Thomas.Database;

namespace Thomas.Cache
{
    public interface ICachedDatabase : IDbResulCachedSet
    {
        void Refresh(string key);
        void Release();
        void Release(string key);
    }

    internal sealed class CachedDatabase : ICachedDatabase
    {
        private readonly IDbDataCache _cache;
        private readonly IDatabase _database;
        private CultureInfo _cultureInfo;

        internal CachedDatabase(IDbDataCache cache, IDatabase database, in CultureInfo cultureInfo)
        {
            _cache = cache;
            _database = database;
            _cultureInfo = cultureInfo;
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

        public T? ToSingle<T>(string script, object? parameters = null, bool refresh = false, string? key = null) where T : class, new()
        {
            var k = !string.IsNullOrEmpty(key) ? key : HashHelper.GenerateHash(script, parameters);

            var fromCache = _cache.TryGet(k, out DictionaryDbQueryItem<T>? result);


            if (!fromCache || refresh)
            {
                var item = _database.ToSingle<T>(script, parameters);
                result = new DictionaryDbQueryItem<T>(script, parameters, item);
                _cache.AddOrUpdate(k, result);
            }

            ReadParameter(script, parameters, fromCache & !refresh);

            return result.Data;
        }

        public IEnumerable<T> ToList<T>(string script, object? parameters = null, bool refresh = false, string? key = null) where T : class, new()
        {
            var k = !string.IsNullOrEmpty(key) ? key : HashHelper.GenerateHash(script, parameters);

            var fromCache = _cache.TryGet(k, out DictionaryDbQueryItem<IEnumerable<T>>? result);

            if (!fromCache || refresh)
            {
                var data = _database.ToList<T>(script, parameters);
                result = new DictionaryDbQueryItem<IEnumerable<T>>(script, parameters, data);
                _cache.AddOrUpdate(k, result);
            }

            ReadParameter(script, parameters, fromCache & !refresh);
            
            return result.Data;
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>> ToTuple<T1, T2>(string script, object? parameters = null, bool refresh = false, string? key = null)
            where T1 : class, new()
            where T2 : class, new()
        {

            var k = !string.IsNullOrEmpty(key) ? key : HashHelper.GenerateHash(script, parameters);

            var fromCache = _cache.TryGet(k, out DictionaryDbQueryItem<Tuple<IEnumerable<T1>, IEnumerable<T2>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2>(script, parameters);
                result = new DictionaryDbQueryItem<Tuple<IEnumerable<T1>, IEnumerable<T2>>>(script, parameters, tuple);
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
            var fromCache = _cache.TryGet(k, out DictionaryDbQueryItem<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2, T3>(script, parameters);
                result = new DictionaryDbQueryItem<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>(script, parameters, tuple);
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

            var fromCache = _cache.TryGet(k, out DictionaryDbQueryItem<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2, T3, T4>(script, parameters);
                result = new DictionaryDbQueryItem<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>(script, parameters, tuple);
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

            var fromCache = _cache.TryGet(k, out DictionaryDbQueryItem<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2, T3, T4, T5>(script, parameters);
                result = new DictionaryDbQueryItem<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>(script, parameters, tuple);
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

            var fromCache = _cache.TryGet(k, out DictionaryDbQueryItem<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2, T3, T4, T5, T6>(script, parameters);
                result = new DictionaryDbQueryItem<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>(script, parameters, tuple);
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

            var fromCache = _cache.TryGet(k, out DictionaryDbQueryItem<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>? result);

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2, T3, T4, T5, T6, T7>(script, parameters);
                result = new DictionaryDbQueryItem<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>(script, parameters, tuple);
                _cache.AddOrUpdate(k, result);
            }

            ReadParameter(script, parameters, fromCache & !refresh);

            return result.Data;
        }

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

        public void Refresh(string key)
        {
            if (DbDataCache.Instance.TryGetNative(key, out IDictionaryDbQueryItem item))
            {
                Type itemType = item.GetType();

                if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(DictionaryDbQueryItem<>))
                {
                    var arguments = itemType.GetGenericArguments();

                    Type genericType = itemType.GetGenericArguments()[0];

                    if (!ReflectionHelper.IsTuple(genericType))
                    {
                        if (ReflectionHelper.IsIEnumerable(genericType))
                        {
                            var elementType = ReflectionHelper.GetIEnumerableElementType(genericType);
                            MethodInfo refreshMethod = GetType().GetMethod("ToList").MakeGenericMethod(elementType);
                            refreshMethod.Invoke(this, new object[] { item.Query, item.Params, true, key });
                        }
                        else
                        {
                            MethodInfo refreshMethod = GetType().GetMethod("ToSingle").MakeGenericMethod(genericType);
                            refreshMethod.Invoke(this, new object[] { item.Query, item.Params, true, key });
                        }

                    }
                    else
                    {

                        var tupleArguments = ReflectionHelper.GetTupleGenericArguments(genericType);
                        var method = GetType().GetMethods().First(x => x.GetGenericArguments().Length == tupleArguments.Length);
                        object _ = tupleArguments.Length switch
                        {
                            2 =>
                                method.MakeGenericMethod(tupleArguments[0], tupleArguments[1])
                                .Invoke(this, new object[] { item.Query, item.Params, true, key }),
                            3 =>
                                method.MakeGenericMethod(tupleArguments[0], tupleArguments[1], tupleArguments[2])
                                .Invoke(this, new object[] { item.Query, item.Params, true, key }),
                            4 =>
                                method.MakeGenericMethod(tupleArguments[0], tupleArguments[1], tupleArguments[2], tupleArguments[3])
                                .Invoke(this, new object[] { item.Query, item.Params, true, key }),
                            5 =>
                                method.MakeGenericMethod(tupleArguments[0], tupleArguments[1], tupleArguments[2], tupleArguments[3], tupleArguments[4])
                                .Invoke(this, new object[] { item.Query, item.Params, true, key }),
                            6 =>
                                method.MakeGenericMethod(tupleArguments[0], tupleArguments[1], tupleArguments[2], tupleArguments[3], tupleArguments[4], tupleArguments[5])
                                .Invoke(this, new object[] { item.Query, item.Params, true, key }),
                            7 =>
                                method.MakeGenericMethod(tupleArguments[0], tupleArguments[1], tupleArguments[2], tupleArguments[3], tupleArguments[4], tupleArguments[5], tupleArguments[6])
                                .Invoke(this, new object[] { item.Query, item.Params, true, key }),
                            _ => throw new InvalidOperationException("Unexpected type in cache.")
                        };

                        _ = null;
                    }

                }
                else
                {
                    throw new InvalidOperationException("Unexpected type in cache.");
                }
            }
            else
            {
                throw new KeyNotFoundException($"Item with key '{key}' not found in cache.");
            }
        }

        #endregion
    }
}
