
using System.Data.Common;
using System.Reflection;
using TData.Core.Provider;

namespace TData.Database
{
    internal static class DatabaseInternalConfiguration
    {
        internal static void SetFetchSizeOracleReader(DbDataReader reader, in int batchSize)
        {
            var rowSizeProperty = DatabaseHelperProvider.OracleDataReader.GetProperty("RowSize", BindingFlags.Public | BindingFlags.Instance).GetGetMethod();
            var fetchSizeProperty = DatabaseHelperProvider.OracleDataReader.GetProperty("FetchSize", BindingFlags.Public | BindingFlags.Instance).GetSetMethod();
            var rowSize = (long)rowSizeProperty.Invoke(reader, null);
            fetchSizeProperty.Invoke(reader, new object[] { batchSize * rowSize });
        }
    }
}
