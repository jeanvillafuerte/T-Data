using System;
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
        private readonly JobStrategy _jobStrategy;
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

        public DbCommand(IDatabaseProvider provider, JobStrategy jobStrategy, ThomasDbStrategyOptions options)
        {
            _provider = provider;
            _jobStrategy = jobStrategy;
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

            Parameters = new IDataParameter[0];

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

            Parameters = new IDataParameter[0];

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

        /// <summary>
        /// Return the result set of the command in a queue of object arrays and the column names array
        /// </summary>
        /// <param name="behavior">Command behavior</param>
        /// <returns></returns>
        //public (Queue<object[]>, string[]) GetRawData(CommandBehavior behavior, int expectedSize = 256)
        //{
        //    _reader = _command.ExecuteReader(behavior);

        //    var columns = GetColumns(_reader);

        //    var queue = new Queue<object[]>(expectedSize);

        //    while (_reader.Read())
        //    {
        //        var values = new object[columns.Length];
        //        _reader.GetValues(values);
        //        queue.Enqueue(values);
        //    }

        //    return (queue, columns);
        //}

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

            return (data, columns);
        }


        public async Task<(object[][], string[])> ReadAsync(CommandBehavior behavior, CancellationToken cancellationToken, int expectedSize = 256)
        {
            _reader = await _command.ExecuteReaderAsync(behavior, cancellationToken);

            var columns = GetColumns(_reader);

            var data = new object[expectedSize][];

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

        private void ResizeArray(ref object[][] data)
        {
            int newcapacity = (int)(data.Length * 200L / 100);

            if (newcapacity < data.Length + 4)
                newcapacity = data.Length + 4;

            object[][] newarray = new object[newcapacity][];

            Array.Copy(data, newarray, data.Length);

            data = newarray;
        }

        public IEnumerable<T> TransformData<T>(object[][] data, Dictionary<string, MetadataPropertyInfo> properties, string[] columns) where T : class, new()
        {
            if (data.Length == 0)
                return Enumerable.Empty<T>();

            return _jobStrategy.FormatData<T>(properties, data, columns);
        }

        private void NextResult() => _reader.NextResult();

        public void RescueOutParamValues()
        {
            if(_reader != null)
                NextResult();

            foreach (var item in Parameters.Where(x => x.Direction == ParameterDirection.Output || x.Direction == ParameterDirection.InputOutput))
                item.Value = _command.Parameters[item.ParameterName].Value;
        }

        public Dictionary<string, MetadataPropertyInfo> GetProperties<T>(string[] columns)
        {
            Type tp = typeof(T);

            var key = tp.FullName!;

            if (MetadataCacheManager.Instance.TryGet(key, out Dictionary<string, MetadataPropertyInfo> properties))
                return properties;

            var props = tp.GetProperties();

            EnsureStrictMode(key, props, columns);

            properties = props.ToDictionary(x => x.Name, y =>
                                 y.PropertyType.IsGenericType ? new MetadataPropertyInfo(y, Nullable.GetUnderlyingType(y.PropertyType)) : new MetadataPropertyInfo(y, y.PropertyType));

            MetadataCacheManager.Instance.Set(key, properties);

            return properties;
        }

        public void EnsureStrictMode(string key, PropertyInfo[] props, string[] columns)
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
    }

}
