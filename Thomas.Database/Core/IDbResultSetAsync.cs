using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Thomas.Database.Core
{
    public interface IDbResultSetAsync
    {
        (Action, IAsyncEnumerable<List<T>>) FetchDataAsync<T>(string script, object parameters, int batchSize = 1000);
        (Action, IAsyncEnumerable<List<T>>) FetchDataAsync<T>(string script, object parameters, int batchSize, CancellationToken cancellationToken);

        Task<int> ExecuteAsync(string script, object parameters = null);
        Task<int> ExecuteAsync(string script, object parameters, CancellationToken cancellationToken);
        Task<T> ExecuteScalarAsync<T>(string script, object parameters = null);
        Task<T> ExecuteScalarAsync<T>(string script, object parameters, CancellationToken cancellationToken);
        Task<T> FetchOneAsync<T>(string script, object parameters = null);
        Task<T> FetchOneAsync<T>(string script, object parameters, CancellationToken cancellationToken);
        Task<List<T>> FetchListAsync<T>(string script, object parameters = null);
        Task<List<T>> FetchListAsync<T>(string script, object parameters, CancellationToken cancellationToken);
        Task<Tuple<List<T1>, List<T2>>> FetchTupleAsync<T1, T2>(string script, object parameters = null);
        Task<Tuple<List<T1>, List<T2>, List<T3>>> FetchTupleAsync<T1, T2, T3>(string script, object parameters = null);
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> FetchTupleAsync<T1, T2, T3, T4>(string script, object parameters = null);
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>> FetchTupleAsync<T1, T2, T3, T4, T5>(string script, object parameters = null);
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>> FetchTupleAsync<T1, T2, T3, T4, T5, T6>(string script, object parameters = null);
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> FetchTupleAsync<T1, T2, T3, T4, T5, T6, T7>(string script, object parameters = null);
        Task<Tuple<List<T1>, List<T2>>> FetchTupleAsync<T1, T2>(string script, object parameters, CancellationToken cancellationToken);
        Task<Tuple<List<T1>, List<T2>, List<T3>>> FetchTupleAsync<T1, T2, T3>(string script, object parameters, CancellationToken cancellationToken);
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> FetchTupleAsync<T1, T2, T3, T4>(string script, object parameters, CancellationToken cancellationToken);
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>> FetchTupleAsync<T1, T2, T3, T4, T5>(string script, object parameters, CancellationToken cancellationToken);
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>> FetchTupleAsync<T1, T2, T3, T4, T5, T6>(string script, object parameters, CancellationToken cancellationToken);
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> FetchTupleAsync<T1, T2, T3, T4, T5, T6, T7>(string script, object parameters, CancellationToken cancellationToken);
    }
}
