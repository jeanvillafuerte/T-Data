using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Thomas.Database.Core
{
    public interface IDbOperationResultAsync
    {
        Task<DbOpAsyncResult> ExecuteOpAsync(string script, object? parameters, CancellationToken cancellationToken);

        Task<DbOpAsyncResult<T>> ToSingleOpAsync<T>(string script, object? parameters, CancellationToken cancellationToken) where T : class, new();

        Task<DbOpAsyncResult<IEnumerable<T>>> ToListOpAsync<T>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken) where T : class, new();

        Task<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>>> ToTupleOpAsync<T1, T2>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
           where T1 : class, new()
           where T2 : class, new();

        Task<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>> ToTupleOp<T1, T2, T3>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new();

        Task<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>> ToTupleOp<T1, T2, T3, T4>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new();

        Task<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>> ToTupleOp<T1, T2, T3, T4, T5>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new();

        Task<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>> ToTupleOp<T1, T2, T3, T4, T5, T6>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new();

        Task<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>> ToTupleOp<T1, T2, T3, T4, T5, T6, T7>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
           where T1 : class, new()
           where T2 : class, new()
           where T3 : class, new()
           where T4 : class, new()
           where T5 : class, new()
           where T6 : class, new()
           where T7 : class, new();
    }
}
