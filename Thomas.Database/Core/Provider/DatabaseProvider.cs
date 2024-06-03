using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using static Thomas.Database.Core.Provider.DatabaseHelperProvider;

namespace Thomas.Database.Core.Provider
{
    internal static class DatabaseProvider
    {
        #region build connection

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static DbConnection CreateConnection(in SqlProvider provider, in string stringConnection)
        {
            ConnectionCache.TryGetValue(provider, out var getConnectionAction);
            return getConnectionAction!(stringConnection);
        }

        public static (Func<object, string, string, DbCommand, DbCommand>, Action<object, DbCommand, DbDataReader>) GetCommandMetaData(in LoaderConfiguration options, in int key, in CommandType commandType, in Type type, in bool buffered, ref CommandBehavior commandBehavior)
        {
            if (!options.IsExecuteNonQuery)
                commandBehavior |= CommandBehavior.SequentialAccess;

            var loadParametersDelegate = GetSetupCommandDelegate(in type, in options, out var hasOutputParams, out var parameters);

            Action<object, DbCommand, DbDataReader> loadOutParameterDelegate = null;
            if (hasOutputParams)
                loadOutParameterDelegate = LoadOutParameterDelegate(in options.IsExecuteNonQuery, in type, in parameters);

            if (buffered)
                DatabaseHelperProvider.CommandMetadata.TryAdd(key, new CommandMetadata(in loadParametersDelegate, in loadOutParameterDelegate, in commandBehavior, in commandType));

            return (loadParametersDelegate, loadOutParameterDelegate);
        }

        public static void RemoveSequentialAccess(in int key)
        {
            if (DatabaseHelperProvider.CommandMetadata.TryGetValue(key, out var metadata))
            {
                DatabaseHelperProvider.CommandMetadata.TryUpdate(key, metadata.CloneNoCommandSequencial(), metadata);
            }
        }

        #endregion build connection

        public static DbCommand CreateCommand(in DbConnection connection, in string script, in CommandType commandType, in int timeout)
        {
            var command = connection.CreateCommand();
            command.CommandText = script;
            command.CommandTimeout = timeout;
            command.CommandType = commandType;

            return command;
        }

        public static bool IsCancelatedOperationException(in Exception exception)
        {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            return exception.Message.AsSpan().Contains("Operation cancelled by user", StringComparison.OrdinalIgnoreCase);
#else
            return exception?.Message.Contains("Operation cancelled by user") ?? false;
#endif
        }

        internal static object GetValueFromOracleParameter(in IDbDataParameter parameter)
        {
            var valueObject = OracleValueParameterProperty.GetValue(parameter);
            var valueType = valueObject.GetType();
            PropertyInfo valueProperty = valueType.GetProperty("Value");
            return valueProperty.GetValue(valueObject);
        }
    }
}
