using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Thomas.Database.Core
{
    public interface IDbResultSetAsync
    {
        Task<int> ExecuteAsync(string script, object? parameters = null, bool noCacheMetadata = false);
        Task<T> ToSingleAsync<T>(string script, object? parameters = null);
        Task<List<T>> ToListAsync<T>(string script, object? parameters = null);
        Task<Tuple<List<T1>, List<T2>>> ToTupleAsync<T1, T2>(string script, object? parameters = null);
        Task<Tuple<List<T1>, List<T2>, List<T3>>> ToTupleAsync<T1, T2, T3>(string script, object? parameters = null);
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> ToTupleAsync<T1, T2, T3, T4>(string script, object? parameters = null);
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>> ToTupleAsync<T1, T2, T3, T4, T5>(string script, object? parameters = null);
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>> ToTupleAsync<T1, T2, T3, T4, T5, T6>(string script, object? parameters = null);
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> ToTupleAsync<T1, T2, T3, T4, T5, T6, T7>(string script, object? parameters = null);

        Task<int> ExecuteAsync(string script, object? parameters, CancellationToken cancellationToken);
        Task<int> ExecuteAsync(string script, object? parameters, bool noCacheMetadata, CancellationToken cancellationToken);

        Task<T> ToSingleAsync<T>(string script, object? parameters, CancellationToken cancellationToken);
        Task<List<T>> ToListAsync<T>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken);

        Task<Tuple<List<T1>, List<T2>>> ToTupleAsync<T1, T2>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken);
        Task<Tuple<List<T1>, List<T2>, List<T3>>> ToTupleAsync<T1, T2, T3>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken);
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> ToTupleAsync<T1, T2, T3, T4>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken);
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>> ToTupleAsync<T1, T2, T3, T4, T5>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken);
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>> ToTupleAsync<T1, T2, T3, T4, T5, T6>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken);
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> ToTupleAsync<T1, T2, T3, T4, T5, T6, T7>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken);
    }
}
