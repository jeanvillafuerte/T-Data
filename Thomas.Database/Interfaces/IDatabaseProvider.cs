using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Thomas.Database
{
    public interface IDatabaseProvider
    {
        DbConnection CreateConnection(string connection);
        DbCommand CreateCommand(DbConnection connection, string script, bool isStoreProcedure);
        Task<DbCommand> CreateCommandAsync(DbConnection connection, string script, bool isStoreProcedure, CancellationToken cancellationToken);
        bool IsCancellatedOperationException(Exception? excetion);
        (IDataParameter[], string) ExtractValuesFromSearchTerm(object searchTerm);
    }
}
