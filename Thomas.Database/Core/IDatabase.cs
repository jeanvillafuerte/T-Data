using System.Collections.Generic;
using Thomas.Database.Core;

namespace Thomas.Database
{
    public interface IDatabase : IDbOperationResult, IDbOperationResultAsync, IDbResulSet, IDbResultSetAsync
    {
        int Execute(string script, object? inputData = null);
        IEnumerable<dynamic> GetMetadataParameter(string script, object? parameters);
    }
}
