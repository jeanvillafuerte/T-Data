using System.Data;
using System.Data.Common;

namespace Thomas.Database
{
    public static class DbObjectExtensions
    {
        public static void Kill(this DbCommand command)
        {
            command.Connection.Close();
            command.Connection.Dispose();
            command.Dispose();
        }

        public static void Kill(this IDataReader reader)
        {
            reader.Close();
            reader.Dispose();
        }

        public static void Kill(this DbConnection connection)
        {
            connection.Close();
            connection.Dispose();
        }
    }
}
