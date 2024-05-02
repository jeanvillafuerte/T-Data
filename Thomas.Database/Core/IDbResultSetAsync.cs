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
        Task<T?> ToSingleAsync<T>(string script, object? parameters = null) where T : class, new();
        Task<List<T>> ToListAsync<T>(string script, object? parameters = null) where T : class, new();
        Task<Tuple<List<T1>, List<T2>>> ToTupleAsync<T1, T2>(string script, object? parameters = null)
            where T1 : class, new()
            where T2 : class, new();
        Task<Tuple<List<T1>, List<T2>, List<T3>>> ToTupleAsync<T1, T2, T3>(string script, object? parameters = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new();

        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> ToTupleAsync<T1, T2, T3, T4>(string script, object? parameters = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new();

        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>> ToTupleAsync<T1, T2, T3, T4, T5>(string script, object? parameters = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new();

        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>> ToTupleAsync<T1, T2, T3, T4, T5, T6>(string script, object? parameters = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new();

        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> ToTupleAsync<T1, T2, T3, T4, T5, T6, T7>(string script, object? parameters = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
            where T7 : class, new();

        Task<int> ExecuteAsync(string script, object? parameters, CancellationToken cancellationToken);
        Task<int> ExecuteAsync(string script, object? parameters, bool noCacheMetadata, CancellationToken cancellationToken);

        Task<T?> ToSingleAsync<T>(string script, object? parameters, CancellationToken cancellationToken) where T : class, new();
        Task<List<T>> ToListAsync<T>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken) where T : class, new();

        Task<Tuple<List<T1>, List<T2>>> ToTupleAsync<T1, T2>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new();

        Task<Tuple<List<T1>, List<T2>, List<T3>>> ToTupleAsync<T1, T2, T3>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new();

        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> ToTupleAsync<T1, T2, T3, T4>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new();

        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>> ToTupleAsync<T1, T2, T3, T4, T5>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new();

        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>> ToTupleAsync<T1, T2, T3, T4, T5, T6>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new();

        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> ToTupleAsync<T1, T2, T3, T4, T5, T6, T7>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
            where T7 : class, new();
    }
}
