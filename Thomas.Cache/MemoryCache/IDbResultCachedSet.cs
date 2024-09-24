using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Thomas.Cache.MemoryCache
{
    public interface IDbResultCachedSet
    {
        T FetchOne<T>(in string script, in object parameters = null, in string key = null, in bool refresh = false);

        List<T> FetchList<T>(in string script, in object parameters = null, in string key = null, in bool refresh = false);

        T FetchOne<T>(in Expression<Func<T, bool>> where = null, in Expression<Func<T, object>> selector = null, in string key = null, in bool refresh = false);

        List<T> FetchList<T>(in Expression<Func<T, bool>> where = null, in Expression<Func<T, object>> selector = null, in string key = null, in bool refresh = false);

        Tuple<List<T1>, List<T2>> FetchTuple<T1, T2>(in string script, in object parameters = null, in string key = null, in bool refresh = false);

        Tuple<List<T1>, List<T2>, List<T3>> FetchTuple<T1, T2, T3>(in string script, in object parameters = null, in string key = null, in bool refresh = false);

        Tuple<List<T1>, List<T2>, List<T3>, List<T4>> FetchTuple<T1, T2, T3, T4>(in string script, in object parameters = null, in string key = null, in bool refresh = false);

        Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>> FetchTuple<T1, T2, T3, T4, T5>(in string script, in object parameters = null, in string key = null, in bool refresh = false);

        Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>> FetchTuple<T1, T2, T3, T4, T5, T6>(in string script, in object parameters = null, in string key = null, in bool refresh = false);

        Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>> FetchTuple<T1, T2, T3, T4, T5, T6, T7>(in string script, in object parameters = null, in string key = null, in bool refresh = false);
    }
}
