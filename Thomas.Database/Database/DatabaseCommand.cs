using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Thomas.Database.Cache.Metadata;
using Thomas.Database.Exceptions;

namespace Thomas.Database
{
    //Preset and Execute database command
    internal sealed class DatabaseCommand : IDisposable
    {
        private static readonly Regex storeProcedureNameRegex = new Regex(@"^(?!EXEC)(?:\[?\w+\]?$|^\[?\w+\]?\.\[?\w+\]?$|\[?\w+\]?\.\[?\w+\]?.\[?\w+\]?)$", RegexOptions.Compiled);

        private readonly IDatabaseProvider? _provider;
        private readonly DbSettings _options;
        private readonly string? _script;
        private readonly string? _metadataKey;
        private readonly string? _metadataInputKey;
        private readonly object? _searchTerm;
        private readonly bool _isStoreProcedure;

        private DbDataReader? _reader;
        private DbCommand? _command;

        private DbConnection? _connection;
        private DbTransaction? _transaction;

        // output parameters
        public IEnumerable<IDbDataParameter> OutParameters
        {
            get
            {

                if (_command.Parameters == null || _command.Parameters.Count == 0)
                {
                    yield break;
                }

                foreach (DbParameter item in _command.Parameters)
                {
                    if (item.Direction == ParameterDirection.InputOutput || item.Direction == ParameterDirection.Output)
                    {
                        yield return item;
                    }
                }

                yield break;
            }
        }

        public DatabaseCommand(in IDatabaseProvider provider, in DbSettings options)
        {
            _provider = provider;
            _options = options;
        }

        public DatabaseCommand(in IDatabaseProvider provider, in DbSettings options, in string script, in object? searchTerm, in DbTransaction transaction = null, in DbCommand command = null)
        {
            _provider = provider;
            _options = options;
            _script = script;
            _searchTerm = searchTerm;
            _transaction = transaction;
            _command = command;
            _connection = transaction?.Connection;

            if (script != null)
            {
                if (_searchTerm != null)
                {
                    _metadataInputKey = HashHelper.GenerateHash(script, in searchTerm);
                }

                _isStoreProcedure = storeProcedureNameRegex.Matches(script).Count > 0;
                _metadataKey = HashHelper.GenerateUniqueHash(script);
            }
        }

        internal DbCommand CreateEmptyCommand()
        {
            return _connection.CreateCommand();
        }

        internal DbTransaction BeginTransaction()
        {
            _connection = _provider.CreateConnection(in _options.StringConnection);
            _connection.Open();
            return _connection.BeginTransaction();
        }

        internal void Prepare()
        {
            if (_connection == null)
            {
                _connection = _provider.CreateConnection(in _options.StringConnection);
                _command = _provider.CreateCommand(in _connection, in _script, in _isStoreProcedure);

                if (_metadataInputKey != null)
                {
                    var parameters = _provider.GetParams(_metadataInputKey, _searchTerm);

                    foreach (var parameter in parameters)
                    {
                        _command.Parameters.Add(parameter);
                    }
                }

                _connection.Open();

                _command.Prepare();
            }
            else
            {
                _command.CommandText = _script;
                _command.CommandTimeout = _options.ConnectionTimeout;
                _command.Transaction = _transaction;

                if (_isStoreProcedure)
                    _command.CommandType = CommandType.StoredProcedure;

                if (_metadataInputKey != null)
                {
                    var parameters = _provider.GetParams(_metadataInputKey, _searchTerm);
                    _command.Parameters.Clear();

                    foreach (var parameter in parameters)
                    {
                        _command.Parameters.Add(parameter);
                    }
                }

                _command.Prepare();
            }
        }

        /// <summary>
        /// responsible for preparing the command to be executed and opening the connection and returning the parameters of the command
        /// </summary>
        /// <param name="script">script or store procedure name</param>
        /// <param name="isStoreProcedure">flagged is script provided is a store procedure</param>
        /// <param name="searchTerm">search term object</param>
        /// <returns>Parameters of the command</returns>
        public async Task PrepareAsync(CancellationToken cancellationToken)
        {
            _connection = _provider.CreateConnection(in _options.StringConnection);
            _command = _provider.CreateCommand(_connection, in _script, in _isStoreProcedure);

            if (_metadataInputKey != null)
            {
                var parameters = _provider.GetParams(_metadataInputKey, _searchTerm);
                foreach (var parameter in parameters)
                {
                    _command.Parameters.Add(parameter);
                }
            }

            await _connection.OpenAsync(cancellationToken);
            await _command.PrepareAsync(cancellationToken);
        }

        /// <summary>
        /// Get the columns of the result set
        /// </summary>
        /// <param name="reader">Data reader</param>
        /// <returns>Column names array</returns>
        /// <exception cref="Exception"></exception>
        private static string[] GetColumns(in DbDataReader reader)
        {
            var count = reader.FieldCount;

            if (count == 0)
                throw new EmptyDataReaderException("Missing fields on result set");

            var cols = new string[count];

            for (int i = 0; i < count; i++)
            {
                cols[i] = reader.GetName(i);
            }

            return cols;
        }

        public int ExecuteNonQuery() => _command.ExecuteNonQuery();

        public async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => await _command.ExecuteNonQueryAsync(cancellationToken);

        private void NextResult() => _reader.NextResult();

        public void SetValuesOutFields()
        {
            if (_reader != null)
                NextResult();

            if (OutParameters.Any())
            {
                foreach (var item in OutParameters)
                    item.Value = _command.Parameters[item.ParameterName].Value;

                _provider.LoadParameterValues(OutParameters, in _searchTerm, in _metadataInputKey);
            }
        }

        private IEnumerable<MetadataPropertyInfo> GetProperties<T>(in string[] columns)
        {
            MetadataPropertyInfo[]? props = null;

            if (CacheResultInfo.TryGet(in _metadataKey, ref props))
                return props!;

            props = new MetadataPropertyInfo[columns.Length];

            var properties = typeof(T).GetProperties();

            int realProperties = 0;

            for (int i = 0; i < columns.Length; i++)
            {
                foreach (var property in properties)
                {
                    if (property.Name.Equals(columns[i], StringComparison.InvariantCultureIgnoreCase))
                    {
                        props[i] = new MetadataPropertyInfo(in property);
                        realProperties++;
                        break;
                    }
                }
            }

            if (columns.Length > realProperties)
            {
                Array.Resize(ref props, realProperties);
            }

            CacheResultInfo.Set(in _metadataKey, in props);

            return props;
        }

        #region reader operations
        public async Task<IEnumerable<T>> ReadListItemsAsync<T>(CommandBehavior behavior, CancellationToken cancellationToken) where T : class, new()
        {
            var list = new List<T>();

            await foreach (var item in ReadItemsAsync<T>(behavior, cancellationToken))
            {
                list.Add(item);
            }

            return list;
        }

        private async IAsyncEnumerable<T> ReadItemsAsync<T>(CommandBehavior behavior, CancellationToken cancellationToken) where T : class, new()
        {
            _reader = await _command.ExecuteReaderAsync(behavior, cancellationToken);

            var columns = GetColumns(in _reader);
            var properties = GetProperties<T>(in columns).ToArray();

            while (await _reader.ReadAsync(cancellationToken))
            {
                T item = new T();
                object[] values = new object[columns.Length];
                _reader.GetValues(values);

                for (int j = 0; j < columns.Length; j++)
                {
                    properties[j].SetValue(item, in values[j], _options.CultureInfo);
                }

                yield return item;
            }
        }

        public async Task<IEnumerable<T>> ReadListNextItemsAsync<T>(CancellationToken cancellationToken) where T : class, new()
        {
            var list = new List<T>();

            await foreach (var item in ReadNextItemsAsync<T>(cancellationToken))
            {
                list.Add(item);
            }

            return list;
        }

        private async IAsyncEnumerable<T> ReadNextItemsAsync<T>(CancellationToken cancellationToken) where T : class, new()
        {
            await _reader.NextResultAsync();
            var columns = GetColumns(in _reader);
            var properties = GetProperties<T>(in columns).ToArray();

            while (await _reader.ReadAsync(cancellationToken))
            {
                T item = new T();
                object[] values = new object[columns.Length];

                _reader.GetValues(values);

                for (int j = 0; j < columns.Length; j++)
                {
                    properties[j].SetValue(item, in values[j], _options.CultureInfo);
                }

                yield return item;
            }
        }

        public IEnumerable<T> ReadListItems<T>(CommandBehavior behavior) where T : class, new()
        {
            var values = new List<T>();

            foreach (var item in ReadItems<T>(behavior))
            {
                values.Add(item);
            }

            return values;
        }

        private IEnumerable<T> ReadItems<T>(CommandBehavior behavior) where T : class, new()
        {
            _reader = _command.ExecuteReader(behavior);

            var columns = GetColumns(in _reader);
            var properties = GetProperties<T>(in columns).ToArray();

            while (_reader.Read())
            {
                T item = new T();
                object[] values = new object[columns.Length];
                _reader.GetValues(values);

                for (int j = 0; j < columns.Length; j++)
                {
                    properties[j].SetValue(item, in values[j], _options.CultureInfo);
                }

                yield return item;
            }
        }

        public IEnumerable<T> ReadListNextItems<T>() where T : class, new()
        {
            var list = new List<T>();
            foreach (var item in ReadNextItems<T>())
            {
                list.Add(item);
            }
            return list;
        }

        private IEnumerable<T> ReadNextItems<T>() where T : class, new()
        {
            _reader.NextResult();
            var columns = GetColumns(in _reader);
            var properties = GetProperties<T>(in columns).ToArray();

            while (_reader.Read())
            {
                T item = new T();
                object[] values = new object[columns.Length];
                _reader.GetValues(values);

                for (int j = 0; j < columns.Length; j++)
                {
                    properties[j].SetValue(item, in values[j], _options.CultureInfo);
                }

                yield return item;
            }
        }

        #endregion
        public void Dispose()
        {
            _reader?.Dispose();

            if (_transaction == null)
            {
                _command?.Dispose();
                _connection?.Dispose();
            }
        }
    }

}
