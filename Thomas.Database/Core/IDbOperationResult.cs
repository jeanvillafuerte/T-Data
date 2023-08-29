using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Thomas.Database.Core
{
    public interface IDbOperationResult
    {
        DataBaseOperationResult ExecuteOp(string script, bool isStoreProcedure = true);
        DataBaseOperationResult ExecuteOp(object inputData, string procedureName);

        DataBaseOperationResult<T> ToSingleOp<T>(string script, bool isStoreProcedure = true) where T : class, new();
        DataBaseOperationResult<T> ToSingleOp<T>(object inputData, string procedureName) where T : class, new();

        DataBaseOperationResult<IEnumerable<T>> ToListOp<T>(string script, bool isStoreProcedure = true) where T : class, new();
        DataBaseOperationResult<IEnumerable<T>> ToListOp<T>(object inputData, string procedureName) where T : class, new();

        Task<DataBaseOperationAsyncResult> ExecuteOpAsync(string script, bool isStoreProcedure, CancellationToken cancellationToken);
        Task<DataBaseOperationAsyncResult> ExecuteOpAsync(object inputData, string procedureName, CancellationToken cancellationToken);

        Task<DataBaseOperationAsyncResult<T>> ToSingleOpAsync<T>(string script, bool isStoreProcedure, CancellationToken cancellationToken) where T : class, new();
        Task<DataBaseOperationAsyncResult<T>> ToSingleOpAsync<T>(object inputData, string procedureName, CancellationToken cancellationToken) where T : class, new();

        Task<DataBaseOperationAsyncResult<IEnumerable<T>>> ToListOpAsync<T>(string script, bool isStoreProcedure, CancellationToken cancellationToken) where T : class, new();
        Task<DataBaseOperationAsyncResult<IEnumerable<T>>> ToListOpAsync<T>(object inputData, string procedureName, CancellationToken cancellationToken) where T : class, new();

        Tuple<IEnumerable<T1>, IEnumerable<T2>> ToTuple<T1, T2>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new();
        Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>> ToTuple<T1, T2, T3>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new();
        Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>> ToTuple<T1, T2, T3, T4>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new();
        Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>> ToTuple<T1, T2, T3, T4, T5>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new();
        Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>> ToTuple<T1, T2, T3, T4, T5, T6>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new();
        Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>> ToTuple<T1, T2, T3, T4, T5, T6, T7>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new() where T7 : class, new();

        DataBaseOperationResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>> ToTupleOp<T1, T2>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new();
        DataBaseOperationResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>> ToTupleOp<T1, T2, T3>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new();
        DataBaseOperationResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>> ToTupleOp<T1, T2, T3, T4>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new();
        DataBaseOperationResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>> ToTupleOp<T1, T2, T3, T4, T5>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new();
        DataBaseOperationResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>> ToTupleOp<T1, T2, T3, T4, T5, T6>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new();
        DataBaseOperationResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>> ToTupleOp<T1, T2, T3, T4, T5, T6, T7>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new() where T7 : class, new();

        DataBaseOperationResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>> ToTupleOp<T1, T2>(object inputData, string procedureName) where T1 : class, new() where T2 : class, new();
        DataBaseOperationResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>> ToTupleOp<T1, T2, T3>(object inputData, string procedureName) where T1 : class, new() where T2 : class, new() where T3 : class, new();
        DataBaseOperationResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>> ToTupleOp<T1, T2, T3, T4>(object inputData, string procedureName) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new();
        DataBaseOperationResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>> ToTupleOp<T1, T2, T3, T4, T5>(object inputData, string procedureName) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new();
        DataBaseOperationResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>> ToTupleOp<T1, T2, T3, T4, T5, T6>(object inputData, string procedureName) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new();
        DataBaseOperationResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>> ToTupleOp<T1, T2, T3, T4, T5, T6, T7>(object inputData, string procedureName) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new() where T7 : class, new();
    }
}
