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

        void ExecuteTransaction(Action<IDatabase> func);
        T ExecuteTransaction<T>(Func<IDatabase, T> func);
        void Commit();
        void Rollback();

        Task<T> ExecuteTransactionAsync<T>(Func<IDatabase, T> func, CancellationToken cancellationToken);
        Task ExecuteTransaction(Action<IDatabase> func, CancellationToken cancellationToken);
        Task CommitAsync();
        Task RollbackAsync();
    }
}
