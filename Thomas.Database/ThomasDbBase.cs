using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using Thomas.Database.Cache;

namespace Thomas.Database
{
    public abstract partial class ThomasDbBase
    {
        #region Fields

        protected IDatabaseProvider Provider;

        protected int MaxDegreeOfParallelism { get; set; } = 1;

        protected string User { get; set; }

        protected SecureString Password { get; set; }

        protected string StringConnection { get; set; }

        protected string CultureInfo { get; set; } = "en-US";

        protected bool DetailErrorMessage { get; set; }

        protected bool StrictMode { get; set; }

        protected bool SensitiveDataLog { get; set; }

        #endregion

        #region Error Handling

        protected string ErrorDetailMessage(string procedureName, IDataParameter[] parameters, Exception excepcion)
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

        protected bool CheckContainNullables(PropertyInfo[] properties)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                if (Nullable.GetUnderlyingType(properties[i].PropertyType) != null)
                {
                    return true;
                }
            }

            return false;
        }

        protected int GetMaxDegreeOfParallelism()
        {
            if (MaxDegreeOfParallelism == 1 || MaxDegreeOfParallelism == 0)
            {
                return 1;
            }
            else
            {
                return Environment.ProcessorCount <= MaxDegreeOfParallelism ? MaxDegreeOfParallelism : 1;
            }
        }

        protected (DbCommand, IDataParameter[]) PreProcessing(string script, bool isStoreProcedure, object searchTerm)
        {
            DbCommand command = null;

            if (!string.IsNullOrEmpty(User) && Password != null && Password.Length > 0)
            {
                command = Provider.CreateCommand(StringConnection, User, Password);
            }
            else
            {
                command = Provider.CreateCommand(StringConnection);
            }

            command.CommandText = script;
            command.CommandType = isStoreProcedure ? CommandType.StoredProcedure : CommandType.Text;
            command.UpdatedRowSource = UpdateRowSource.None;
            command.Prepare();

            IDataParameter[] parameters = null;

            if (searchTerm != null)
            {
                parameters = Provider.ExtractValuesFromSearchTerm(searchTerm);

                for (int i = 0; i < parameters.Length; i++)
                {
                    command.Parameters.Add(parameters[i]);
                }
            }

            return (command, parameters);
        }

        protected string[] GetColumns(IDataReader listReader)
        {
            var count = listReader.FieldCount;
            var cols = new string[count];

            for (int i = 0; i < count; i++)
            {
                cols[i] = listReader.GetName(i).ToUpper();
            }

            return cols;
        }

        #endregion

        #region Transformation 1

        internal T[] FormatDataWithNullables<T>(object[][] data,
                                              IDictionary<string, InfoProperty> properties,
                                              string[] columns,
                                              int length) where T : new()
        {

            T[] list = new T[data.Length];
            CultureInfo culture = new CultureInfo(CultureInfo);

            foreach (var item in GetItemsWithNullables(data, length, properties, columns, culture))
            {
                list[item.Item2] = item.Item1;
            }

            return list;

            IEnumerable<(T, int)> GetItemsWithNullables(object[][] data,
                               int length,
                               IDictionary<string, InfoProperty> properties,
                               string[] columns,
                               CultureInfo culture)
            {
                object[] v = null;

                for (int i = 0; i < length; i++)
                {
                    T item = new();
                    v = data[i];
                    yield return (GetItemWithNullables(item, length, properties, columns, v, culture), i);
                }
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T GetItemWithNullables<T>(T item, int length,
                                  IDictionary<string, InfoProperty> properties,
                                  string[] columns,
                                  object[] v,
                                  CultureInfo culture)
        {
            for (int j = 0; j < length; j++)
            {
                if (v[j] is DBNull) 
                { 
                    continue; 
                }
                properties[columns[j]].Info.SetValue(item, Convert.ChangeType(v[j], properties[columns[j]].Type), BindingFlags.Default, null, null, culture);
            }

            return item;
        }



        #endregion

        #region Transformation 2

        internal T[] FormatDataWithoutNullables<T>(object[][] data,
                                      IDictionary<string, InfoProperty> properties,
                                      string[] columns, int length) where T : new()
        {
            T[] list = new T[data.Length];

            CultureInfo culture = new CultureInfo(CultureInfo);

            foreach (var item in GetWithoutNullablesItems<T>(data, length, properties, columns, culture))
            {
                list[item.Item2] = item.Item1;
            }

            return list;

        }

        IEnumerable<(T, int)> GetWithoutNullablesItems<T>(object[][] data,
                                          int length,
                                          IDictionary<string, InfoProperty> properties,
                                          string[] columns,
                                          CultureInfo culture) where T : new()
        {
            object[] v = null;

            for (int i = 0; i < length; i++)
            {
                T item = new();
                v = data[i];
                yield return (GetItemWithoutNullables(item, length, properties, columns, v, culture), i);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T GetItemWithoutNullables<T>(T item, int length,
                  IDictionary<string, InfoProperty> properties,
                  string[] columns,
                  object[] v,
                  CultureInfo culture)
        {

            for (int j = 0; j < length; j++)
            {
                if (v[j] is DBNull)
                {
                    continue;
                }

                properties[columns[j]].Info.SetValue(item, v[j], BindingFlags.Default, null, null, culture);
            }

            return item;
        }

        #endregion

        #region Extract data
        protected object[][] ExtractData(IDataReader reader, int columnCount)
        {
            object[] values = new object[columnCount];

            var list = new List<object[]>();

            foreach (var item in GetValues(reader, values))
            {
                list.Add(item);
            }

            return list.ToArray();
        }

        protected ConcurrentDictionary<int, object[]> ExtractData2(IDataReader reader, int columnCount, int processorCount)
        {
            object[] values = new object[columnCount];

            var dictionaryValues = new ConcurrentDictionary<int, object[]>(processorCount, 1024);

            int index = 0;

            foreach (var item in GetValues(reader, values))
            {
                dictionaryValues[index] = item;
                index++;
            }

            return dictionaryValues;
        }

        protected IEnumerable<object[]> GetValues(IDataReader reader, object[] values)
        {
            while (reader.Read())
            {
                reader.GetValues(values);
                yield return values;
            }
        }

        #endregion

    }
}
