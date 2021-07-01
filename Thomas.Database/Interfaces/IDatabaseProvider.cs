using System.Data;
using System.Data.Common;
using System.Security;

namespace Thomas.Database
{
    public interface IDatabaseProvider
    {
        DbConnection CreateConnection(string connection);
        DbConnection CreateConnection(string connection, string user, SecureString password);
        DbCommand CreateCommand(string connection);
        DbCommand CreateCommand(string connection, string user, SecureString password);
        DbTransaction CreateTransacion(string stringConnection);
        DbTransaction CreateTransacion(DbConnection connection);
        DbParameter CreateParameter(string parameterName, object value, DbType type);
        IDataParameter[] ExtractValuesFromSearchTerm(object searchTerm);
    }
}
