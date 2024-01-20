using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Thomas.Database.Core;

namespace Thomas.Database
{
    public interface IDatabase : IDbOperationResult, IDbOperationResultAsync, IDbResulSet, IDbResultSetAsync
    {
        int Execute(string script, object? inputData = null);
        IEnumerable<dynamic> GetMetadataParameter(string script, object? parameters);

        bool ExecuteTransaction(Func<IDatabase, TransactionResult> func);
        T ExecuteTransaction<T>(Func<IDatabase, T> func);
        TransactionResult Commit();
        TransactionResult Rollback();

        Task<T> ExecuteTransactionAsync<T>(Func<IDatabase, T> func, CancellationToken cancellationToken);
        Task<bool> ExecuteTransaction(Func<IDatabase, TransactionResult> func, CancellationToken cancellationToken);
        Task<TransactionResult> CommitAsync();
        Task<TransactionResult> RollbackAsync();
    }
}
