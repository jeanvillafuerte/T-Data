using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Thomas.Database
{
    public static class DbObjectExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Kill(this DbCommand command)
        {
            command?.Connection?.Close();
            command?.Connection?.Dispose();
            command?.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Kill(this IDataReader reader)
        {
            reader?.Close();
            reader?.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Kill(this DbConnection connection)
        {
            connection?.Close();
            connection?.Dispose();
        }
    }
}
