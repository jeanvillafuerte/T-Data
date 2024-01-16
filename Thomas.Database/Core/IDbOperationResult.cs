using System;
using System.Collections.Generic;

namespace Thomas.Database.Core
{
    public interface IDbOperationResult
    {
        DbOpResult ExecuteOp(string script, object? parameters = null);

        DbOpResult<T> ToSingleOp<T>(string script, object? parameters = null) where T : class, new();

        DbOpResult<IEnumerable<T>> ToListOp<T>(string script, object? parameters = null) where T : class, new();

        DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>> ToTupleOp<T1, T2>(string script, object? parameters = null) 
            where T1 : class, new() 
            where T2 : class, new();

        DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>> ToTupleOp<T1, T2, T3>(string script, object? parameters = null) 
            where T1 : class, new() 
            where T2 : class, new() 
            where T3 : class, new();

        DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>> ToTupleOp<T1, T2, T3, T4>(string script, object? parameters) 
            where T1 : class, new() 
            where T2 : class, new() 
            where T3 : class, new() 
            where T4 : class, new();

        DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>> ToTupleOp<T1, T2, T3, T4, T5>(string script, object? parameters = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new();

        DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>> ToTupleOp<T1, T2, T3, T4, T5, T6>(string script, object? parameters = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new();

        DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>> ToTupleOp<T1, T2, T3, T4, T5, T6, T7>(string script, object? parameters = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
            where T7 : class, new();
    }
}
