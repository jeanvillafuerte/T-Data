using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Thomas.Database.Core
{
    public interface IDbResulSet
    {
        T? ToSingle<T>(string script, bool isStoreProcedure = true) where T : class, new();
        T? ToSingle<T>(object inputData, string procedureName) where T : class, new();
        IEnumerable<T> ToList<T>(string script, bool isStoreProcedure = true) where T : class, new();
        IEnumerable<T> ToList<T>(object inputData, string procedureName) where T : class, new();

        Task<T?> ToSingleAsync<T>(string script, bool isStoreProcedure, CancellationToken cancellationToken) where T : class, new();
        Task<T?> ToSingleAsync<T>(object inputData, string procedureName, CancellationToken cancellationToken) where T : class, new();
        Task<IEnumerable<T>> ToListAsync<T>(string script, bool isStoreProcedure, CancellationToken cancellationToken) where T : class, new();
        Task<IEnumerable<T>> ToListAsync<T>(object inputData, string procedureName, CancellationToken cancellationToken) where T : class, new();

        //Tuple<IEnumerable<T1>, IEnumerable<T2>> ToTuple<T1, T2>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new();
        //Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>> ToTuple<T1, T2, T3>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new();
        //Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>> ToTuple<T1, T2, T3, T4>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new();
        //Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>> ToTuple<T1, T2, T3, T4, T5>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new();
        //Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>> ToTuple<T1, T2, T3, T4, T5, T6>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new();
        //Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>> ToTuple<T1, T2, T3, T4, T5, T6, T7>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new() where T7 : class, new();
    }
}
