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
            DatabaseHelperProvider.ConnectionCache.TryGetValue(provider, out var getConnectionAction);
            return getConnectionAction!(stringConnection);
        }

        public static Action<object, DbCommand> GetCommandMetadata(in LoaderConfiguration options, in int key, in CommandType commandType, in Type type, in bool noCache, in bool canCloseConnection, ref bool hasOutputParams, ref CommandBehavior commandBehavior)
        {
            if (type == null)
            {
                if (!noCache)
                    DatabaseHelperProvider.CommandMetadata.TryAdd(key, new CommandMetadata(null!, false, in commandBehavior, in commandType));

                return null;
            }

            Action<object, DbCommand>? loadParametersDelegate = GetLoadCommandParametersDelegate(in type, in options, ref hasOutputParams);

            if (!hasOutputParams && canCloseConnection)
                commandBehavior |= CommandBehavior.CloseConnection;

            if (!noCache)
                DatabaseHelperProvider.CommandMetadata.TryAdd(key, new CommandMetadata(in loadParametersDelegate, in hasOutputParams, in commandBehavior, in commandType));

            return loadParametersDelegate;
        }

        #endregion

        public static DbCommand CreateCommand(in DbConnection connection, in string script, in CommandType commandType, in int timeout)
        {
            var command = connection.CreateCommand();
            command.CommandText = script;
            command.CommandTimeout = timeout;
            command.CommandType = commandType;

            return command;
        }

        public static bool IsCancellatedOperationException(in Exception? exception)
        {
            ReadOnlySpan<char> message = exception != null ? exception.Message.AsSpan() : ReadOnlySpan<char>.Empty;
            return message.Contains("Operation cancelled by user", StringComparison.OrdinalIgnoreCase);
        }


        internal static object GetValueFromOracleParameter(in IDbDataParameter parameter)
        {
            var valueObject = DatabaseHelperProvider.OracleValueParameterProperty.GetValue(parameter);
            var valueType = valueObject.GetType();
            PropertyInfo valueProperty = valueType.GetProperty("Value");
            return valueProperty.GetValue(valueObject);
        }
    }
}
