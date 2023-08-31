using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Thomas.Database.Core
{
    public interface IDbOperationResult
    {
        DbOpResult ExecuteOp(string script, bool isStoreProcedure = true);
        DbOpResult ExecuteOp(object inputData, string procedureName);

        DbOpResult<T> ToSingleOp<T>(string script, bool isStoreProcedure = true) where T : class, new();
        DbOpResult<T> ToSingleOp<T>(object inputData, string procedureName) where T : class, new();

        DbOpResult<IEnumerable<T>> ToListOp<T>(string script, bool isStoreProcedure = true) where T : class, new();
        DbOpResult<IEnumerable<T>> ToListOp<T>(object inputData, string procedureName) where T : class, new();

        Task<DbOpAsyncResult> ExecuteOpAsync(string script, bool isStoreProcedure, CancellationToken cancellationToken);
        Task<DbOpAsyncResult> ExecuteOpAsync(object inputData, string procedureName, CancellationToken cancellationToken);

        Task<DbOpAsyncResult<T>> ToSingleOpAsync<T>(string script, bool isStoreProcedure, CancellationToken cancellationToken) where T : class, new();
        Task<DbOpAsyncResult<T>> ToSingleOpAsync<T>(object inputData, string procedureName, CancellationToken cancellationToken) where T : class, new();

        Task<DbOpAsyncResult<IEnumerable<T>>> ToListOpAsync<T>(string script, bool isStoreProcedure, CancellationToken cancellationToken) where T : class, new();
        Task<DbOpAsyncResult<IEnumerable<T>>> ToListOpAsync<T>(object inputData, string procedureName, CancellationToken cancellationToken) where T : class, new();

        DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>> ToTupleOp<T1, T2>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new();
        DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>> ToTupleOp<T1, T2, T3>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new();
        DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>> ToTupleOp<T1, T2, T3, T4>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new();
        DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>> ToTupleOp<T1, T2, T3, T4, T5>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new();
        DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>> ToTupleOp<T1, T2, T3, T4, T5, T6>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new();
        DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>> ToTupleOp<T1, T2, T3, T4, T5, T6, T7>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new() where T7 : class, new();

        DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>> ToTupleOp<T1, T2>(object inputData, string procedureName) where T1 : class, new() where T2 : class, new();
        DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>> ToTupleOp<T1, T2, T3>(object inputData, string procedureName) where T1 : class, new() where T2 : class, new() where T3 : class, new();
        DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>> ToTupleOp<T1, T2, T3, T4>(object inputData, string procedureName) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new();
        DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>> ToTupleOp<T1, T2, T3, T4, T5>(object inputData, string procedureName) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new();
        DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>> ToTupleOp<T1, T2, T3, T4, T5, T6>(object inputData, string procedureName) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new();
        DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>> ToTupleOp<T1, T2, T3, T4, T5, T6, T7>(object inputData, string procedureName) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new() where T7 : class, new();
    }
}
