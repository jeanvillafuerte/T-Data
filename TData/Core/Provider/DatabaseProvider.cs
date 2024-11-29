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
        
        public delegate DbCommand ConfigureCommandDelegate(in object command, in string connectionString, in string commandText, in DbCommand existingCommand);
        public delegate DbCommand ConfigureCommandDelegate2(in object[] command, in string connectionString, in string commandText, in DbCommand existingCommand);

        public static (ConfigureCommandDelegate, Action<object, DbCommand, DbDataReader>) GetCommandMetaData(in LoaderConfiguration options, in bool isExecuteNonQuery, in Type type, in bool addPagingParams, ref DbParameterInfo[] parameters)
        {
            var loadParametersDelegate = (ConfigureCommandDelegate)GetSetupCommandDelegate(true, in type, in options, in addPagingParams, out var hasOutputParams, ref parameters);

            Action<object, DbCommand, DbDataReader> loadOutParameterDelegate = null;

            if (hasOutputParams)
                loadOutParameterDelegate = LoadOutParameterDelegate(in isExecuteNonQuery, in type, in parameters, in options.Provider);

            return (loadParametersDelegate, loadOutParameterDelegate);
        }

        public static ConfigureCommandDelegate2 GetCommandMetaData2(in LoaderConfiguration options, in DbParameterInfo[] parameters)
        {
            DbParameterInfo[] localParameters = parameters;
            return (ConfigureCommandDelegate2)GetSetupCommandDelegate(false, null, in options, false, out _, ref localParameters);
        }

        #endregion build connection

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
