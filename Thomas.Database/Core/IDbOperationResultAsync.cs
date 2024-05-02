using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Thomas.Database.Core
{
    public interface IDbOperationResultAsync
    {
        /// <summary>
        /// Execute a script and return the result as a DbOpAsyncResult
        /// </summary>
        /// <param name="script">Sql text</param>
        /// <param name="parameters">Object containing param values matching script tokens</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        Task<DbOpAsyncResult> ExecuteOpAsync(string script, object? parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Execute a script and return the result as a DbOpAsyncResult with no cache metadata, useful for unique executions
        /// </summary>
        /// <param name="script">Sql Text</param>
        /// <param name="parameters">Object containing param values matching script tokens</param>
        /// <param name="noCacheMetadata">Flag to avoid save metadata information useful for unique execution</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        Task<DbOpAsyncResult> ExecuteOpAsync(string script, object? parameters, bool noCacheMetadata, CancellationToken cancellationToken);

        Task<DbOpAsyncResult<T>> ToSingleOpAsync<T>(string script, object? parameters, CancellationToken cancellationToken) where T : class, new();

        Task<DbOpAsyncResult<List<T>>> ToListOpAsync<T>(string script, object? parameters, CancellationToken cancellationToken) where T : class, new();

        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>>>> ToTupleOpAsync<T1, T2>(string script, object? parameters, CancellationToken cancellationToken)
           where T1 : class, new()
           where T2 : class, new();

        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>>>> ToTupleOp<T1, T2, T3>(string script, object? parameters, CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new();

        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>>> ToTupleOp<T1, T2, T3, T4>(string script, object? parameters, CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new();

        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>>> ToTupleOp<T1, T2, T3, T4, T5>(string script, object? parameters, CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new();

        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>>> ToTupleOp<T1, T2, T3, T4, T5, T6>(string script, object? parameters, CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new();

        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>>> ToTupleOp<T1, T2, T3, T4, T5, T6, T7>(string script, object? parameters, CancellationToken cancellationToken)
           where T1 : class, new()
           where T2 : class, new()
           where T3 : class, new()
           where T4 : class, new()
           where T5 : class, new()
           where T6 : class, new()
           where T7 : class, new();
    }
}
