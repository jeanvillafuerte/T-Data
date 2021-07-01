using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Thomas.Database
{
    public abstract class ThomasDbBase
    {
        #region fields

        protected IDatabaseProvider Provider;

        protected int MaxDegreeOfParallelism { get; set; } = 1;

        protected TypeMatchConvention Convention { get; set; }

        protected string User { get; set; }

        protected SecureString Password { get; set; }

        protected string StringConnection { get; set; }

        protected string CultureInfo { get; set; } = "en-US";

        protected bool DetailErrorMessage { get; set; }

        protected bool StrictMode { get; set; }

        protected bool SensitiveDataLog { get; set; }

        #endregion

        #region Extract and process data

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected string[] GetColumns(IDataReader listReader)
        {
            var count = listReader.FieldCount;
            var cols = new string[count];
            for (int i = 0; i < count; i++)
            {
                switch (Convention)
                {
                    case TypeMatchConvention.CapitalLetter: var column = listReader.GetName(i)?.ToLower(); cols[i] = column.Substring(0, 1).ToUpper() + column.Substring(1); break;
                    case TypeMatchConvention.LowerCase: cols[i] = listReader.GetName(i)?.ToLower(); break;
                    case TypeMatchConvention.UpperCase: cols[i] = listReader.GetName(i)?.ToUpper(); break;
                    case TypeMatchConvention.Default: cols[i] = listReader.GetName(i); break;
                }
            }
            return cols;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected string SanitizeName(string name)
        {
            switch (Convention)
            {
                case TypeMatchConvention.CapitalLetter:
                    return name.Substring(0).ToUpper() + name.Substring(1).ToLower();
                case TypeMatchConvention.LowerCase:
                    return name.ToLower();
                case TypeMatchConvention.UpperCase:
                    return name.ToUpper();
                case TypeMatchConvention.Default:
                    return name;
            }

            return name;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected object[][] ExtractData(IDataReader reader, int columnCount)
        {
            object[] values = new object[columnCount];

            var list = new List<object[]>(1024);

            foreach (var item in GetValues(reader, values))
            {
                list.Add(item);
            }

            return list.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected ConcurrentDictionary<int, object[]> ExtractData2(IDataReader reader, int columnCount)
        {
            object[] values = new object[columnCount];

            var dictionaryValues = new ConcurrentDictionary<int, object[]>();

            int index = 0;

            foreach (var item in GetValues(reader, values))
            {
                dictionaryValues[index] = item;
                index++;
            }

            return dictionaryValues;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerable<object[]> GetValues(IDataReader reader, object[] values)
        {
            while (reader.Read())
            {
                reader.GetValues(values);
                yield return values;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected T GetItem<T>(T item, int length, PropertyInfo prop,
                                  Dictionary<string, PropertyInfo> properties,
                                  string[] columns,
                                  object[] v)
        {
            for (int j = 0; j < length; j++)
            {
                if (v[j] is DBNull) { continue; }
                prop = properties[columns[j]];
                prop.SetValue(item, v[j]);
            }

            return item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected IEnumerable<(T, int)> GetItems<T>(object[][] data,
                                           int length,
                                           PropertyInfo prop,
                                           Dictionary<string, PropertyInfo> properties,
                                           string[] columns,
                                           object[] v) where T : new()
        {
            for (int i = 0; i < length; i++)
            {
                T item = new T();
                v = data[i];
                yield return (GetItem<T>(item, length, prop, properties, columns, v), i);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected T GetItemForParallel<T>(T item, int length, PropertyInfo prop,
                                  ConcurrentDictionary<string, PropertyInfo> properties,
                                  string[] columns,
                                  object[] v)
        {
            for (int j = 0; j < length; j++)
            {
                if (v[j] is DBNull) { continue; }
                prop = properties[columns[j]];
                prop.SetValue(item, v[j]);
            }

            return item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected IEnumerable<(T, int)> GetItemsForParallel<T>(Dictionary<int, object[]> data,
                                           int length,
                                           PropertyInfo prop,
                                           ConcurrentDictionary<string, PropertyInfo> properties,
                                           string[] columns) where T : new()
        {
            foreach (var d in data)
            {
                T item = new T();
                yield return (GetItemForParallel<T>(item, length, prop, properties, columns, d.Value), d.Key);
            }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] FormatDataWithoutNullables<T>(object[][] data,
                                              Dictionary<string, PropertyInfo> properties,
                                              string[] columns, int length) where T : new()
        {
            T[] list = new T[data.Length];
            object[] v = null;
            PropertyInfo prop = null;

            foreach (var item in GetItems<T>(data, length, prop, properties, columns, v))
            {
                list[item.Item2] = item.Item1;
            }

            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] FormatDataWithoutNullablesParallel<T>(ConcurrentDictionary<int, object[]> data,
                                              ConcurrentDictionary<string, PropertyInfo> properties,
                                              string[] columns, int length) where T : new()
        {
            int processors = GetMaxDegreeOfParallelism();

            int pageSize = data.Count == 1 ? 1 : data.Count / processors;

            if (pageSize == 1 || processors <= 1)
            {
                var dataArray = data.Select(s => s.Value).ToArray();
                var props = properties.ToDictionary(x => x.Key, y => y.Value);

                return FormatDataWithoutNullables<T>(dataArray, props, columns, length);
            }

            int page = 1;
            int localLen = length;

            ConcurrentDictionary<int, Dictionary<int, object[]>> masterList = new ConcurrentDictionary<int, Dictionary<int, object[]>>();

            int mod = 0;

            for (int i = 0; i < processors; i++)
            {
                if (i + 1 == processors)
                {
                    mod = data.Count % processors;
                }

                var insideList = data.Skip((page - 1) * pageSize).Take(pageSize + mod);

                masterList[page - 1] = new Dictionary<int, object[]>(insideList);
                page++;
            }

            ConcurrentDictionary<int, T> listResult = new ConcurrentDictionary<int, T>(processors, data.Count);

            Parallel.For(0, processors, (i) =>
            {
                var splitData = masterList[i];

                PropertyInfo prop = null;

                foreach (var item in GetItemsForParallel<T>(splitData, length, prop, properties, columns))
                {
                    listResult[item.Item2] = item.Item1;
                }
            });

            return listResult.Select(x => x.Value).ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] FormatDataWithNullables<T>(object[][] data,
                                              Dictionary<string, PropertyInfo> properties,
                                              string[] columns, int length) where T : new()
        {
            T[] list = new T[data.Length];
            object[] v = null;
            PropertyInfo prop = null;

            foreach (var item in GetItems<T>(data, length, prop, properties, columns, v))
            {
                list[item.Item2] = item.Item1;
            }

            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] FormatDataWithNullablesParallel<T>(ConcurrentDictionary<int, object[]> data,
                                              ConcurrentDictionary<string, PropertyInfo> properties,
                                              string[] columns, int length) where T : new()
        {
            var localLen = length;

            var processors = GetMaxDegreeOfParallelism();

            var list = new ConcurrentDictionary<int, T>(processors, data.Count);

            var culture = new CultureInfo(CultureInfo);

            Parallel.For(0, data.Count, (i) =>
            {
                T item = new T();

                object[] v = data[i];

                PropertyInfo prop;
                Type propType;
                //Diccionario Property Type para asignacion directa
                for (int j = 0; j < localLen; j++)
                {
                    if (v[j] is DBNull) { continue; }
                    prop = properties[columns[j]];
                    propType = prop.PropertyType.IsGenericType ? Nullable.GetUnderlyingType(prop.PropertyType) : prop.PropertyType;
                    prop.SetValue(item, Convert.ChangeType(v[j], propType, culture));
                }

                list[i] = item;

                v = null;
                prop = null;

            });

            return list.Select(s => s.Value).ToArray();
        }

        #endregion

        protected void SetConvention(TypeMatchConvention convention)
        {
            Convention = convention;
        }

        protected bool MatchColumnConvention(string propertyName, string columnName)
        {
            switch (Convention)
            {
                case TypeMatchConvention.CapitalLetter:
                    return propertyName.ToUpper() + propertyName.Substring(1) == columnName;
                case TypeMatchConvention.LowerCase:
                    return propertyName.ToLower() == columnName;
                case TypeMatchConvention.UpperCase:
                    return propertyName.ToUpper() == columnName;
                case TypeMatchConvention.Default:
                    return propertyName == columnName;
                default:
                    return false;
            }
        }

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

        protected bool CheckContainNullables(Type type)
        {
            PropertyInfo[] properties = type.GetProperties();

            for (int i = 0; i < properties.Length; i++)
            {
                if (Nullable.GetUnderlyingType(properties[i].PropertyType) != null)
                {
                    return true;
                }
            }

            return false;
        }

    }
}
