using System;
using System.Data;
using System.Data.Common;
using System.Security;
using System.Text;

namespace Thomas.Database
{
    public abstract class DbBase
    {
        #region Fields

        protected IDatabaseProvider Provider { get; set; }

        protected int MaxDegreeOfParallelism { get; set; } = 1;

        protected string User { get; set; } = string.Empty;

        protected SecureString? Password { get; set; }

        protected string StringConnection { get; set; } = string.Empty;

        protected string CultureInfo { get; set; } = "";

        protected bool DetailErrorMessage { get; set; }

        protected bool StrictMode { get; set; }

        protected bool SensitiveDataLog { get; set; }

        protected int TimeOut { get; set; }

        #endregion

        #region Error Handling

        protected string ErrorDetailMessage(string procedureName, IDataParameter[]? parameters, Exception excepcion)
        {
            if (!DetailErrorMessage)
            {
                return excepcion.Message;
            }

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Store Procedure:");
            stringBuilder.AppendLine("\t" + procedureName);

            if (parameters != null && !SensitiveDataLog)
            {
                stringBuilder.AppendLine("Parameters:");

                foreach (var parameter in parameters)
                {
                    stringBuilder.AppendLine("\t" + parameter.ParameterName + " : " + (parameter.Value is DBNull ? "NULL" : parameter.Value) + " ");
                }
            }

            stringBuilder.AppendLine("Exception Message:");
            stringBuilder.AppendLine("\t" + excepcion.Message);

            if (excepcion.InnerException != null)
            {
                stringBuilder.AppendLine("Inner Exception Message :");
                stringBuilder.AppendLine("\t" + excepcion.InnerException);
            }

            stringBuilder.AppendLine();
            return stringBuilder.ToString();
        }

        protected string ErrorDetailMessage(string scriptRaw, Exception excepcion)
        {
            if (!DetailErrorMessage)
            {
                return excepcion.Message;
            }

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Script :");
            stringBuilder.AppendLine("\t" + scriptRaw);
            stringBuilder.AppendLine("Exception Message:");
            stringBuilder.AppendLine("\t" + excepcion.Message);

            if (excepcion.InnerException != null)
            {
                stringBuilder.AppendLine("Inner Exception Message :");
                stringBuilder.AppendLine("\t" + excepcion.InnerException);
            }

            stringBuilder.AppendLine();

            return stringBuilder.ToString();
        }

        #endregion

        #region Util

        protected DbCommand PreProcessing(string script, bool isStoreProcedure)
        {
            var command = Provider.CreateCommand(script, isStoreProcedure);

            command.Connection.Open();

            return command;
        }

        protected (DbCommand, IDataParameter[]?) PreProcessing(string script, bool isStoreProcedure, object searchTerm)
        {
            IDataParameter[]? parameters = null;

            var command = Provider.CreateCommand(script, isStoreProcedure);

            if (searchTerm != null)
            {
                command.Parameters.Clear();
                parameters = Provider.ExtractValuesFromSearchTerm(searchTerm);

                for (int i = 0; i < parameters.Length; i++)
                {
                    command.Parameters.Add(parameters[i]);
                }
            }

            command.Connection.Open();

            return (command, parameters);
        }

        protected string[] GetColumns(IDataReader listReader)
        {
            var count = listReader.FieldCount;
            var cols = new string[count];

            for (int i = 0; i < count; i++)
            {
                cols[i] = listReader.GetName(i);
            }

            return cols;
        }

        #endregion

    }
}
