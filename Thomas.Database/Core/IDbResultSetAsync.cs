using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;

namespace Thomas.Database.Core
{
    public interface IDbResultSetAsync
    {
        Task<int> ExecuteAsync(string script, object? parameters, CancellationToken cancellationToken);

        Task<T?> ToSingleAsync<T>(string script, object? parameters, CancellationToken cancellationToken) where T : class, new();

        Task<IEnumerable<T>> ToListAsync<T>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken) where T : class, new();

        Task<Tuple<IEnumerable<T1>, IEnumerable<T2>>> ToTupleAsync<T1, T2>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new();

        Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>> ToTupleAsync<T1, T2, T3>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new();

        Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>> ToTupleAsync<T1, T2, T3, T4>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new();

        Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>> ToTupleAsync<T1, T2, T3, T4, T5>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new() 
            where T3 : class, new() 
            where T4 : class, new() 
            where T5 : class, new();

        Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>> ToTupleAsync<T1, T2, T3, T4, T5, T6>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken) 
            where T1 : class, new() 
            where T2 : class, new() 
            where T3 : class, new() 
            where T4 : class, new() 
            where T5 : class, new() 
            where T6 : class, new();

        Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>> ToTupleAsync<T1, T2, T3, T4, T5, T6, T7>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken) 
            where T1 : class, new() 
            where T2 : class, new() 
            where T3 : class, new() 
            where T4 : class, new() 
            where T5 : class, new() 
            where T6 : class, new() 
            where T7 : class, new();
    }
}
