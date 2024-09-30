using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TData.Core.Converters;
using static TData.Core.Provider.DatabaseHelperProvider;

namespace TData.Core.Provider
{
    internal static class DatabaseProvider
    {
        #region build connection

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static DbConnection CreateConnection(in SqlProvider provider, in string stringConnection) => ConnectionCache[provider](stringConnection);
        

        public static (Func<object, string, string, DbCommand, DbCommand>, Action<object, DbCommand, DbDataReader>) GetCommandMetaData(in LoaderConfiguration options, in bool isExecuteNonQuery, in Type type, ref DbParameterInfo[] parameters)
        {
            var loadParametersDelegate = (Func<object, string, string, DbCommand, DbCommand>)GetSetupCommandDelegate(in type, in options, out var hasOutputParams, ref parameters);

            Action<object, DbCommand, DbDataReader> loadOutParameterDelegate = null;

            if (hasOutputParams)
                loadOutParameterDelegate = LoadOutParameterDelegate(in isExecuteNonQuery, in type, in parameters, in options.Provider);

            return (loadParametersDelegate, loadOutParameterDelegate);
        }

        public static Func<object[], string, string, DbCommand, DbCommand> GetCommandMetaData2(in LoaderConfiguration options, in DbParameterInfo[] parameters)
        {
            DbParameterInfo[] localParameters = parameters;
            return (Func<object[], string, string, DbCommand, DbCommand>)GetSetupCommandDelegate(null, in options, out var hasOutputParams, ref localParameters);
        }

        public static void RemoveSequentialAccess(in int key)
        {
            if (DatabaseHelperProvider.CommandMetadata.TryGetValue(key, out var metadata))
            {
                DatabaseHelperProvider.CommandMetadata.TryUpdate(key, metadata.CloneNoCommandSequential(), metadata);
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
            var message = exception.Message.AsSpan();
            return message.Contains("Operation cancelled by user", StringComparison.OrdinalIgnoreCase)
                || message.Contains("ORA-01013", StringComparison.OrdinalIgnoreCase);
#else
            return exception.Message.Contains("Operation cancelled by user") || exception.Message.Contains("ORA-01013");
#endif
        }

        internal static object GetValueFromOracleParameter(IDbDataParameter parameter, Type targetType)
        {
            var valueObject = OracleValueParameterProperty.GetValue(parameter);
            var valueType = valueObject.GetType();
            PropertyInfo valueProperty = valueType.GetProperty("Value");
            var paramValue = valueProperty.GetValue(valueObject);
            return TypeConversionRegistry.ConvertOutParameterValue(SqlProvider.Oracle, paramValue, targetType, true);
        }

        internal static string TransformPostgresScript(in string routineName, in DbParameterInfo[] parameters, in bool isExecuteNonQuery)
        {
            var hasInputParameters = parameters.Any(x => x.Direction == ParameterDirection.Input || x.Direction == ParameterDirection.InputOutput);

            var prefix = isExecuteNonQuery ? "CALL" : "SELECT * FROM";

            if (hasInputParameters)
            {
                var routineParams = string.Join(",", parameters.Where(x => x.Direction == ParameterDirection.Input || x.Direction == ParameterDirection.InputOutput).Select(x => x.BindName ?? $"@{x.Name.ToLower()}").ToArray());
                return $"{prefix} {routineName}({routineParams})";
            }
            else
            {
                return $"{prefix} {routineName}()";
            }
        }
    }
}
