using System;
using System.Collections.Generic;

namespace Thomas.Database.Core
{
    public interface IDbOperationResult
    {
        DbOpResult ExecuteOp(in string script, in object? parameters = null, in bool noCacheMetadata = false);
        DbOpResult<T> ToSingleOp<T>(in string script, in object? parameters = null);
        DbOpResult<List<T>> ToListOp<T>(in string script, in object? parameters = null);
        DbOpResult<Tuple<List<T1>, List<T2>>> ToTupleOp<T1, T2>(in string script, in object? parameters = null);
        DbOpResult<Tuple<List<T1>, List<T2>, List<T3>>> ToTupleOp<T1, T2, T3>(in string script, in object? parameters = null);
        DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> ToTupleOp<T1, T2, T3, T4>(in string script, in object? parameters);
        DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>> ToTupleOp<T1, T2, T3, T4, T5>(in string script, in object? parameters = null);
        DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>> ToTupleOp<T1, T2, T3, T4, T5, T6>(in string script, in object? parameters = null);
        DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> ToTupleOp<T1, T2, T3, T4, T5, T6, T7>(in string script, in object? parameters = null);
    }
}
