using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Thomas.Cache.MemoryCache;
using Thomas.Database;

namespace Thomas.Cache
{
    public interface ICachedDatabase : IDbResulCachedSet
    {
        void ReleaseCache();
        void ReleaseResult(string script);
        void ReleaseResult(string script, object inputData);
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

        public T? ToSingle<T>(string script, object? parameters = null, bool refresh = false) where T : class, new()
        {
            var queryIdentifier = HashHelper.GenerateHash($"cache_query_{script}", parameters);

            var fromCache = _cache.TryGet(queryIdentifier, out IEnumerable<T> result);

            T? item;

            if (!fromCache || refresh)
            {
                item = _database.ToSingle<T>(script, parameters);
                result = new[] { item };

                _cache.AddOrUpdate(queryIdentifier, result);
            }
            else
            {
                item = result.FirstOrDefault();
            }

            ReadParameter(script, parameters, fromCache & !refresh);

            return item;
        }

        public IEnumerable<T> ToList<T>(string script, object? parameters = null, bool refresh = false) where T : class, new()
        {
            var queryIdentifier = HashHelper.GenerateHash($"cache_query_{script}", parameters);

            var fromCache = _cache.TryGet<T>(queryIdentifier, out IEnumerable<T> result);

            if (!fromCache || refresh)
            {
                result = _database.ToList<T>(script, parameters);

                _cache.AddOrUpdate(queryIdentifier, result);
            }

            ReadParameter(script, parameters, fromCache & !refresh);

            return result;
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>> ToTuple<T1, T2>(string script, object? parameters = null, bool refresh = false)
            where T1 : class, new()
            where T2 : class, new()
        {
            var queryIdentifierT1 = HashHelper.GenerateHash($"cache_query_t1_{script}", parameters);
            var queryIdentifierT2 = HashHelper.GenerateHash($"cache_query_t2_{script}", parameters);

            IEnumerable<T2> t2;

            var fromCache = _cache.TryGet(queryIdentifierT1, out IEnumerable<T1> t1); 

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2>(script, parameters);
                t1 = tuple.Item1;
                t2 = tuple.Item2;

                _cache.AddOrUpdate(queryIdentifierT1, t1);
                _cache.AddOrUpdate(queryIdentifierT2, t2);
            }
            else
            {
                _cache.TryGet(queryIdentifierT2, out t2);
            }

            ReadParameter(script, parameters, fromCache & !refresh);

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>>(t1, t2);
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>> ToTuple<T1, T2, T3>(string script, object? parameters = null, bool refresh = false)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
        {
            var queryIdentifierT1 = HashHelper.GenerateHash($"cache_query_t1_{script}", parameters);
            var queryIdentifierT2 = HashHelper.GenerateHash($"cache_query_t2_{script}", parameters);
            var queryIdentifierT3 = HashHelper.GenerateHash($"cache_query_t3_{script}", parameters);

            IEnumerable<T2> t2;
            IEnumerable<T3> t3;

            var fromCache = _cache.TryGet(queryIdentifierT1, out IEnumerable<T1> t1);

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2, T3>(script, parameters);
                t1 = tuple.Item1;
                t2 = tuple.Item2;
                t3 = tuple.Item3;

                _cache.AddOrUpdate(queryIdentifierT1, t1);
                _cache.AddOrUpdate(queryIdentifierT2, t2);
                _cache.AddOrUpdate(queryIdentifierT3, t3);
                
            }
            else
            {
                _cache.TryGet(queryIdentifierT2, out t2);
                _cache.TryGet(queryIdentifierT3, out t3);
            }

            ReadParameter(script, parameters, fromCache & !refresh);

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>(t1, t2, t3);
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>> ToTuple<T1, T2, T3, T4>(string script, object? parameters = null, bool refresh = false)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
        {
            var queryIdentifierT1 = HashHelper.GenerateHash($"cache_query_t1_{script}", parameters);
            var queryIdentifierT2 = HashHelper.GenerateHash($"cache_query_t2_{script}", parameters);
            var queryIdentifierT3 = HashHelper.GenerateHash($"cache_query_t3_{script}", parameters);
            var queryIdentifierT4 = HashHelper.GenerateHash($"cache_query_t4_{script}", parameters);

            IEnumerable<T2> t2;
            IEnumerable<T3> t3;
            IEnumerable<T4> t4;

            var fromCache = _cache.TryGet<T1>(queryIdentifierT1, out IEnumerable<T1> t1);

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2, T3, T4>(script, parameters);
                t1 = tuple.Item1;
                t2 = tuple.Item2;
                t3 = tuple.Item3;
                t4 = tuple.Item4;

                _cache.AddOrUpdate(queryIdentifierT1, t1);
                _cache.AddOrUpdate(queryIdentifierT2, t2);
                _cache.AddOrUpdate(queryIdentifierT3, t3);
                _cache.AddOrUpdate(queryIdentifierT4, t4);
            }
            else
            {
                _cache.TryGet(queryIdentifierT2, out t2);
                _cache.TryGet(queryIdentifierT3, out t3);
                _cache.TryGet(queryIdentifierT4, out t4);
            }

            ReadParameter(script, parameters, fromCache & !refresh);

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>(t1, t2, t3, t4);
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>> ToTuple<T1, T2, T3, T4, T5>(string script, object? parameters = null, bool refresh = false)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
        {
            var queryIdentifierT1 = HashHelper.GenerateHash($"cache_query_t1_{script}", parameters);
            var queryIdentifierT2 = HashHelper.GenerateHash($"cache_query_t2_{script}", parameters);
            var queryIdentifierT3 = HashHelper.GenerateHash($"cache_query_t3_{script}", parameters);
            var queryIdentifierT4 = HashHelper.GenerateHash($"cache_query_t4_{script}", parameters);
            var queryIdentifierT5 = HashHelper.GenerateHash($"cache_query_t5_{script}", parameters);

            IEnumerable<T2> t2;
            IEnumerable<T3> t3;
            IEnumerable<T4> t4;
            IEnumerable<T5> t5;

            var fromCache = _cache.TryGet<T1>(queryIdentifierT1, out IEnumerable<T1> t1);

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2, T3, T4, T5>(script, parameters);
                t1 = tuple.Item1;
                t2 = tuple.Item2;
                t3 = tuple.Item3;
                t4 = tuple.Item4;
                t5 = tuple.Item5;

                _cache.AddOrUpdate(queryIdentifierT1, t1);
                _cache.AddOrUpdate(queryIdentifierT2, t2);
                _cache.AddOrUpdate(queryIdentifierT3, t3);
                _cache.AddOrUpdate(queryIdentifierT4, t4);
                _cache.AddOrUpdate(queryIdentifierT5, t5);
            }
            else
            {
                _cache.TryGet(queryIdentifierT2, out t2);
                _cache.TryGet(queryIdentifierT3, out t3);
                _cache.TryGet(queryIdentifierT4, out t4);
                _cache.TryGet(queryIdentifierT5, out t5);
            }

            ReadParameter(script, parameters, fromCache & !refresh);

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>(t1, t2, t3, t4, t5);
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>> ToTuple<T1, T2, T3, T4, T5, T6>(string script, object? parameters = null, bool refresh = false)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
        {
            var queryIdentifierT1 = HashHelper.GenerateHash($"cache_query_t1_{script}", parameters);
            var queryIdentifierT2 = HashHelper.GenerateHash($"cache_query_t2_{script}", parameters);
            var queryIdentifierT3 = HashHelper.GenerateHash($"cache_query_t3_{script}", parameters);
            var queryIdentifierT4 = HashHelper.GenerateHash($"cache_query_t4_{script}", parameters);
            var queryIdentifierT5 = HashHelper.GenerateHash($"cache_query_t5_{script}", parameters);
            var queryIdentifierT6 = HashHelper.GenerateHash($"cache_query_t6_{script}", parameters);

            IEnumerable<T2> t2;
            IEnumerable<T3> t3;
            IEnumerable<T4> t4;
            IEnumerable<T5> t5;
            IEnumerable<T6> t6;

            var fromCache = _cache.TryGet<T1>(queryIdentifierT1, out IEnumerable<T1> t1);

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2, T3, T4, T5, T6>(script, parameters);
                t1 = tuple.Item1;
                t2 = tuple.Item2;
                t3 = tuple.Item3;
                t4 = tuple.Item4;
                t5 = tuple.Item5;
                t6 = tuple.Item6;

                _cache.AddOrUpdate(queryIdentifierT1, t1);
                _cache.AddOrUpdate(queryIdentifierT2, t2);
                _cache.AddOrUpdate(queryIdentifierT3, t3);
                _cache.AddOrUpdate(queryIdentifierT4, t4);
                _cache.AddOrUpdate(queryIdentifierT5, t5);
                _cache.AddOrUpdate(queryIdentifierT6, t6);
            }
            else
            {
                _cache.TryGet(queryIdentifierT2, out t2);
                _cache.TryGet(queryIdentifierT3, out t3);
                _cache.TryGet(queryIdentifierT4, out t4);
                _cache.TryGet(queryIdentifierT5, out t5);
                _cache.TryGet(queryIdentifierT6, out t6);
            }

            ReadParameter(script, parameters, fromCache & !refresh);

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>(t1, t2, t3, t4, t5, t6);
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>> ToTuple<T1, T2, T3, T4, T5, T6, T7>(string script, object? parameters = null, bool refresh = false)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
            where T7 : class, new()
        {
            var queryIdentifierT1 = HashHelper.GenerateHash($"cache_query_t1_{script}", parameters);
            var queryIdentifierT2 = HashHelper.GenerateHash($"cache_query_t2_{script}", parameters);
            var queryIdentifierT3 = HashHelper.GenerateHash($"cache_query_t3_{script}", parameters);
            var queryIdentifierT4 = HashHelper.GenerateHash($"cache_query_t4_{script}", parameters);
            var queryIdentifierT5 = HashHelper.GenerateHash($"cache_query_t5_{script}", parameters);
            var queryIdentifierT6 = HashHelper.GenerateHash($"cache_query_t6_{script}", parameters);
            var queryIdentifierT7 = HashHelper.GenerateHash($"cache_query_t7_{script}", parameters);

            IEnumerable<T2> t2;
            IEnumerable<T3> t3;
            IEnumerable<T4> t4;
            IEnumerable<T5> t5;
            IEnumerable<T6> t6;
            IEnumerable<T7> t7;

            var fromCache = _cache.TryGet<T1>(queryIdentifierT1, out IEnumerable<T1> t1);

            if (!fromCache || refresh)
            {
                var tuple = _database.ToTuple<T1, T2, T3, T4, T5, T6, T7>(script, parameters);
                t1 = tuple.Item1;
                t2 = tuple.Item2;
                t3 = tuple.Item3;
                t4 = tuple.Item4;
                t5 = tuple.Item5;
                t6 = tuple.Item6;
                t7 = tuple.Item7;

                _cache.AddOrUpdate(queryIdentifierT1, t1);
                _cache.AddOrUpdate(queryIdentifierT2, t2);
                _cache.AddOrUpdate(queryIdentifierT3, t3);
                _cache.AddOrUpdate(queryIdentifierT4, t4);
                _cache.AddOrUpdate(queryIdentifierT5, t5);
                _cache.AddOrUpdate(queryIdentifierT6, t6);
                _cache.AddOrUpdate(queryIdentifierT7, t7);
            }
            else
            {
                _cache.TryGet(queryIdentifierT2, out t2);
                _cache.TryGet(queryIdentifierT3, out t3);
                _cache.TryGet(queryIdentifierT4, out t4);
                _cache.TryGet(queryIdentifierT5, out t5);
                _cache.TryGet(queryIdentifierT6, out t6);
                _cache.TryGet(queryIdentifierT7, out t7);
            }

            ReadParameter(script, parameters, fromCache & !refresh);

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>(t1, t2, t3, t4, t5, t6, t7);
        }

        public void ReleaseResult(string script)
        {
            var queryIdentifier = HashHelper.GenerateUniqueHash($"cache_query_{script}");
            _cache.Release(queryIdentifier);
        }

        public void ReleaseResult(string script, object inputData)
        {
            var queryIdentifier = HashHelper.GenerateUniqueHash($"cache_query_{script}");
            var inputIdentifies = HashHelper.GenerateUniqueHash($"cache_params_{script}");
            _cache.Release(queryIdentifier);
            DbParameterCache.Instance.Release(inputIdentifies);
        }

        public void ReleaseCache()
        {
            _cache.Clear();
            DbParameterCache.Instance.Clear();
        }
    }
}
