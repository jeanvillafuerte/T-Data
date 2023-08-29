using System.Threading;
using System.Threading.Tasks;
using Thomas.Database.Core;

namespace Thomas.Database
{
    public interface IDatabase : IDbOperationResult, IDbResulSet
    {
        int Execute(string script, bool isStoreProcedure = true);
        int Execute(object inputData, string procedureName);

        Task<int> ExecuteAsync(string script, bool isStoreProcedure, CancellationToken cancellationToken);
        Task<int> ExecuteAsync(object inputData, string procedureName, CancellationToken cancellationToken);
    }
}
