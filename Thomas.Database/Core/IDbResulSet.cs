using System;
using System.Collections.Generic;

namespace Thomas.Database.Core
{
    public interface IDbResultSet
    {
        T FetchOne<T>(in string script, in object parameters = null);
        List<T> FetchList<T>(in string script, in object parameters = null);
        Tuple<List<T1>, List<T2>> FetchTuple<T1, T2>(in string script, in object parameters = null);
        Tuple<List<T1>, List<T2>, List<T3>> FetchTuple<T1, T2, T3>(in string script, in object parameters = null);
        Tuple<List<T1>, List<T2>, List<T3>, List<T4>> FetchTuple<T1, T2, T3, T4>(in string script, in object parameters = null);
        Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>> FetchTuple<T1, T2, T3, T4, T5>(in string script, in object parameters = null);
        Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>> FetchTuple<T1, T2, T3, T4, T5, T6>(in string script, in object parameters = null);
        Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>> FetchTuple<T1, T2, T3, T4, T5, T6, T7>(in string script, in object parameters = null);
    }
}
