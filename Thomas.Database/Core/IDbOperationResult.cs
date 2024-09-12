using System;
using System.Collections.Generic;

namespace Thomas.Database.Core
{
    public interface IDbOperationResult
    {
        DbOpResult TryExecute(in string script, in object parameters = null);
        DbOpResult<T> TryExecuteScalar<T>(in string script, in object parameters = null);
        DbOpResult<T> TryFetchOne<T>(in string script, in object parameters = null);
        DbOpResult<List<T>> TryFetchList<T>(in string script, in object parameters = null);
        DbOpResult<Tuple<List<T1>, List<T2>>> TryFetchTuple<T1, T2>(in string script, in object parameters = null);
        DbOpResult<Tuple<List<T1>, List<T2>, List<T3>>> TryFetchTuple<T1, T2, T3>(in string script, in object parameters = null);
        DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> TryFetchTuple<T1, T2, T3, T4>(in string script, in object parameters);
        DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>> TryFetchTuple<T1, T2, T3, T4, T5>(in string script, in object parameters = null);
        DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>> TryFetchTuple<T1, T2, T3, T4, T5, T6>(in string script, in object parameters = null);
        DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> TryFetchTuple<T1, T2, T3, T4, T5, T6, T7>(in string script, in object parameters = null);
    }
}
