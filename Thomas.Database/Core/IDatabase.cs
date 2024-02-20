using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Thomas.Database.Core;
using Thomas.Database.Core.WriteDatabase;

namespace Thomas.Database
{
    public interface IDatabase : IWriteOnlyDatabase, IDbOperationResult, IDbOperationResultAsync, IDbResulSet, IDbResultSetAsync, IDbSetExpression
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
