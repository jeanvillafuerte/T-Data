using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Thomas.Database
{
    public interface IDatabaseProvider
    {
        DbConnection CreateConnection(in string connection);
        DbCommand CreateCommand(in DbConnection connection, in string script, in bool isStoreProcedure);
        bool IsCancellatedOperationException(in Exception? excetion);
        IEnumerable<IDbDataParameter> GetParams(string metadataKey, object searchTerm);
        void LoadParameterValues(IEnumerable<IDbDataParameter> parameters, in object searchTerm, in string metadataKey);
        IEnumerable<dynamic> GetParams(string metadataKey);
    }
}
