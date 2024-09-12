using System;
using System.Collections.Generic;
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
        Task<DbOpAsyncResult> TryExecuteAsync(string script, object parameters, CancellationToken cancellationToken);

        Task<DbOpAsyncResult<T>> TryExecuteScalarAsync<T>(string script, object parameters = null);
        Task<DbOpAsyncResult<T>> TryExecuteScalarAsync<T>(string script, object parameters, CancellationToken cancellationToken);

        Task<DbOpAsyncResult<T>> TryFetchOneAsync<T>(string script, object parameters, CancellationToken cancellationToken);
        Task<DbOpAsyncResult<List<T>>> TryFetchListAsync<T>(string script, object parameters, CancellationToken cancellationToken);
        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>>>> TryFetchTupleAsync<T1, T2>(string script, object parameters, CancellationToken cancellationToken);
        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>>>> TryFetchTuple<T1, T2, T3>(string script, object parameters, CancellationToken cancellationToken);
        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>>> TryFetchTuple<T1, T2, T3, T4>(string script, object parameters, CancellationToken cancellationToken);
        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>>> TryFetchTuple<T1, T2, T3, T4, T5>(string script, object parameters, CancellationToken cancellationToken);
        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>>> TryFetchTuple<T1, T2, T3, T4, T5, T6>(string script, object parameters, CancellationToken cancellationToken);
        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>>> TryFetchTuple<T1, T2, T3, T4, T5, T6, T7>(string script, object parameters, CancellationToken cancellationToken);
    }
}
