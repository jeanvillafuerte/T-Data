using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Thomas.Database.Cache.Metadata;
using Thomas.Database.Exceptions;
using Thomas.Database.Strategy;

namespace Thomas.Database.Database
{

    //this class is responsible for executing the command and returning the result set
    public class DbCommand : IDisposable
    {
        private static Func<IDataParameter, bool> filter = (IDataParameter x) => x.Direction == ParameterDirection.Output || x.Direction == ParameterDirection.InputOutput;

        private readonly IDatabaseProvider _provider;
        private string _searchTermName;
        private DbDataReader _reader;
        private readonly ThomasDbStrategyOptions _options;
        private readonly CultureInfo _cultureInfo;

        // object to get the values ​​of the output parameters
        private object _searchTerm { get; set; }

        // parameters of the command
        public IDataParameter[] Parameters { get; set; }

        // output parameters
        public IDataParameter[] OutParameters
        {
            get
            {
                return Parameters.Where(filter).ToArray();
            }
        }

        private System.Data.Common.DbCommand _command;
        private DbConnection _connection;

        public DbCommand(IDatabaseProvider provider, ThomasDbStrategyOptions options)
        {
            _provider = provider;
            _options = options;
            _cultureInfo = new CultureInfo(options.Culture);
        }

        // prepare the command to be executed and open the connection
        public void Prepare(string script, bool isStoreProcedure)
        {
            _connection = _provider.CreateConnection(_options.StringConnection);
            _command = _provider.CreateCommand(_connection, script, isStoreProcedure);
            _connection.Open();
        }

        public async Task PrepareAsync(string script, bool isStoreProcedure, CancellationToken cancellationToken)
        {
            _connection = _provider.CreateConnection(_options.StringConnection);
            _command = await _provider.CreateCommandAsync(_connection, script, isStoreProcedure, cancellationToken);
            await _connection.OpenAsync(cancellationToken);
        }

        public IDataParameter[] Prepare(string script, bool isStoreProcedure, object searchTerm)
        {
            _connection = _provider.CreateConnection(_options.StringConnection);
            _command = _provider.CreateCommand(_connection, script, isStoreProcedure);
            _searchTerm = searchTerm;

            Parameters = Array.Empty<IDataParameter>();

            if (searchTerm != null)
            {
                _command.Parameters.Clear();
                (Parameters, _searchTermName) = _provider.ExtractValuesFromSearchTerm(searchTerm);
                _command.Parameters.AddRange(Parameters);
            }

            _connection.Open();
            return Parameters;
        }

        /// <summary>
        /// responsible for preparing the command to be executed and opening the connection and returning the parameters of the command
        /// </summary>
        /// <param name="script">script or store procedure name</param>
        /// <param name="isStoreProcedure">flagged is script provided is a store procedure</param>
        /// <param name="searchTerm">search term object</param>
        /// <returns>Parameters of the command</returns>
        public async Task<IDataParameter[]> PrepareAsync(string script, bool isStoreProcedure, object searchTerm, CancellationToken cancellationToken)
        {
            _connection = _provider.CreateConnection(_options.StringConnection);
            _command = await _provider.CreateCommandAsync(_connection, script, isStoreProcedure, cancellationToken);
            _searchTerm = searchTerm;

            Parameters = Array.Empty<IDataParameter>();

            if (searchTerm != null)
            {
                _command.Parameters.Clear();
                (Parameters, _searchTermName) = _provider.ExtractValuesFromSearchTerm(searchTerm);
                _command.Parameters.AddRange(Parameters);
            }

            await _connection.OpenAsync(cancellationToken);
            return Parameters;
        }

        /// <summary>
        /// Get the columns of the result set
        /// </summary>
        /// <param name="listReader">Data reader</param>
        /// <returns>Column names array</returns>
        /// <exception cref="Exception"></exception>
        private string[] GetColumns(IDataReader listReader)
        {
            var count = listReader.FieldCount;

            if (count == 0)
                throw new EmptyDataReaderException("Missing fields on result set");

            var cols = new string[count];

            for (int i = 0; i < count; i++)
            {
                cols[i] = listReader.GetName(i);
            }

            return cols;
        }

        public int ExecuteNonQuery() => _command.ExecuteNonQuery();

        public async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => await _command.ExecuteNonQueryAsync(cancellationToken);

        public (object[][], string[]) Read(CommandBehavior behavior, int expectedSize = 256)
        {
            _reader = _command.ExecuteReader(behavior);
            return GetRawData(expectedSize);
        }

        public (object[][], string[]) ReadNext(int size = 256)
        {
            NextResult();
            return GetRawData(size);
        }

        private (object[][], string[]) GetRawData(int size)
        {
            var columns = GetColumns(_reader);

            var data = new object[size][];
            var index = 0;

            while (_reader.Read())
            {
                var values = new object[columns.Length];
                _reader.GetValues(values);

                if (index + 1 == data.Length)
                    ResizeArray(ref data);

                data[index++] = values;
            }

            if (data.Length > index)
                Array.Resize(ref data, index);

            return (data.ToArray(), columns);
        }

        public async Task<(object[][], string[])> ReadAsync(CommandBehavior behavior, CancellationToken cancellationToken, int expectedSize = 256)
        {
            _reader = await _command.ExecuteReaderAsync(behavior, cancellationToken);
            return await GetRawDataAsync(cancellationToken, expectedSize);
        }

        public async Task<(object[][], string[])> ReadNextAsync(CancellationToken cancellationToken, int size = 256)
        {
            await NextResultAsync(cancellationToken);
            return await GetRawDataAsync(cancellationToken, size);
        }

        private async Task<(object[][], string[])> GetRawDataAsync(CancellationToken cancellationToken, int size)
        {
            var columns = GetColumns(_reader);

            var data = new object[size][];

            var index = 0;

            while (await _reader.ReadAsync(cancellationToken))
            {
                var values = new object[columns.Length];
                _reader.GetValues(values);

                if (index + 1 == data.Length)
                    ResizeArray(ref data);

                data[index++] = values;
            }

            if (data.Length > index)
                Array.Resize(ref data, index);

            return (data, columns);
        }

        private static void ResizeArray(ref object[][] data)
        {
            int newcapacity = (int)(data.Length * 200L / 100);

            if (newcapacity < data.Length + 4)
                newcapacity = data.Length + 4;

            object[][] newarray = new object[newcapacity][];

            Array.Copy(data, newarray, data.Length);

            data = newarray;
        }

        public IEnumerable<T> TransformData<T>(object[][] data, string[] columns) where T : class, new()
        {
            if (data.Length == 0)
                return Enumerable.Empty<T>();

            var properties = GetProperties<T>();

            if (_options.ThresholdParallelism > data.Length)
                return FormatData<T>(properties, data, columns);
            else
                return FormatDataParallel<T>(properties, data, columns);

        }

        private void NextResult() => _reader.NextResult();

        private async Task NextResultAsync(CancellationToken cancellationToken) => await _reader.NextResultAsync(cancellationToken);

        public void RescueOutParamValues()
        {
            if (_reader != null)
                NextResult();

            foreach (var item in Parameters.Where(x => x.Direction == ParameterDirection.Output || x.Direction == ParameterDirection.InputOutput))
                item.Value = _command.Parameters[item.ParameterName].Value;
        }

        private Dictionary<string, MetadataPropertyInfo> GetProperties<T>()
        {
            Type tp = typeof(T);

            var key = tp.FullName!;

            if (MetadataCacheManager.Instance.TryGet(key, out Dictionary<string, MetadataPropertyInfo> properties))
                return properties;

            var props = tp.GetProperties();

            //EnsureStrictMode(key, props, columns);

            properties = props.ToDictionary(x => x.Name, y =>
                                 y.PropertyType.IsGenericType ? new MetadataPropertyInfo(y, Nullable.GetUnderlyingType(y.PropertyType)) : new MetadataPropertyInfo(y, y.PropertyType));

            MetadataCacheManager.Instance.Set(key, properties);

            return properties;
        }

        private void EnsureStrictMode(string key, PropertyInfo[] props, string[] columns)
        {
            if (_options.StrictMode)
            {
                string[] propsName = new string[props.Length];

                for (int i = 0; i < props.Length; i++)
                {
                    propsName[i] = props[i].Name;
                }

                var propertiesNotFound = columns.Where(x => !propsName.Contains(x, StringComparer.OrdinalIgnoreCase)).ToArray();

                if (propertiesNotFound.Length > 0)
                {
                    _connection.Close();

                    throw new FieldsNotFoundException("There are columns doesn't match with entity's fields. " +
                      "Columns : " + string.Join(", ", propertiesNotFound) + Environment.NewLine +
                      "Entity Name : " + key + Environment.NewLine +
                      "Script : " + "");
                }
            }
        }

        public void SetValuesOutFields()
        {
            if (_searchTerm != null && MetadataCacheManager.Instance.TryGet(_searchTermName, out Dictionary<string, MetadataPropertyInfo> properties))
            {
                foreach (var item in properties)
                {
                    if (item.Value.IsOutParameter)
                    {
                        var parameter = Parameters.FirstOrDefault(x => x.ParameterName == $"@{item.Value.ParameterName}");
                        item.Value.SetValue(_searchTerm, parameter.Value, _cultureInfo);
                    }
                }
            }
        }

        public void CloseConnetion()
        {
            _connection.Close();
        }

        public async Task CloseConnetionAsync()
        {
            await _connection.CloseAsync();
        }

        public void Dispose()
        {
            _reader?.Dispose();
            _command?.Dispose();
            _connection?.Dispose();
        }

        #region data treatment

        public IEnumerable<T> FormatData<T>(Dictionary<string, MetadataPropertyInfo> props, object[][] data, string[] columns) where T : class, new()
        {
            var length = data.Length;

            T[] list = new T[length];

            for (int i = 0; i < length; i++)
            {
                T item = new T();

                for (int j = 0; j < columns.Length; j++)
                    props[columns[j]].SetValue(item, data[i][j], _cultureInfo);

                list[i] = item;
            }

            return list;
        }

        public IEnumerable<T> FormatDataParallel<T>(Dictionary<string, MetadataPropertyInfo> props, object[][] data, string[] columns) where T : class, new()
        {
            int pageSize = data.Length / Environment.ProcessorCount;

            var main = new ConcurrentDictionary<int, (CultureInfo, object[][])>(1, Environment.ProcessorCount);

            int mod = 0;

            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                if (i + 1 == Environment.ProcessorCount)
                {
                    mod = data.Length % Environment.ProcessorCount;
                }

                main.TryAdd(i, ((CultureInfo)_cultureInfo.Clone(), data.Skip(i * pageSize).Take(pageSize + mod).Select(x => x).ToArray()));
            }

            var listResult = new ConcurrentDictionary<int, T[]>(Environment.ProcessorCount, data.Length);

            Parallel.For(0, Environment.ProcessorCount, (i) =>
            {
                if (main.TryGetValue(i, out var tuple))
                {
                    var data = tuple.Item2;
                    var cultureInfo = tuple.Item1;

                    var length = data.Length;
                    var list = new T[length];

                    for (int j = 0; j < length; j++)
                    {
                        T item = new T();

                        for (int k = 0; k < columns.Length; k++)
                            props[columns[k]].SetValue(item, data[j][k], _cultureInfo);

                        list[j] = item;
                    }

                    listResult.TryAdd(i, list);
                }
            });

            return listResult.OrderBy(pair => pair.Key).SelectMany(x => x.Value);
        }

        #endregion
    }

}
