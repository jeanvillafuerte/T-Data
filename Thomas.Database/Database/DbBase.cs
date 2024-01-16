using System;
using System.Data;
using System.Text;
using Thomas.Database.Exceptions;

namespace Thomas.Database
{
    public abstract class DbBase
    {
        #region Fields

        protected IDatabaseProvider Provider { get; set; }

        protected DbSettings Options { get; set; }

        #endregion

        #region Error Handling

        protected string ErrorDetailMessage(string procedureName, IDataParameter[]? parameters, Exception excepcion)
        {
            if (!Options.DetailErrorMessage)
            {
                return excepcion.Message;
            }

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Store Procedure:");
            stringBuilder.AppendLine("\t" + procedureName);

            if (parameters != null && !Options.HideSensibleDataValue)
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
            if (!Options.DetailErrorMessage)
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

        protected string[] GetColumns(IDataReader listReader)
        {
            var count = listReader.FieldCount;

            if (count == 0)
                throw new EmptyDataReaderException("Not fields defined on result set");

            var cols = new string[count];

            for (int i = 0; i < count; i++)
                cols[i] = listReader.GetName(i);

            return cols;
        }

        #endregion
    }
}
