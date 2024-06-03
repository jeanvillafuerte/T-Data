using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Thomas.Database.Core.Provider;
using Thomas.Database.Database;
using Thomas.Database.Configuration;

using static Thomas.Database.Core.Provider.DatabaseHelperProvider;

namespace Thomas.Database
{
    internal partial class DatabaseCommand : IDisposable, IAsyncDisposable
    {
        private readonly CommandType _commandType;
        private readonly string _script;
        private readonly string _connectionString;
        private readonly SqlProvider _provider;
        private readonly int _timeout;
        private readonly object _filter;
        private readonly int _preparationQueryKey;
        private readonly int _operationKey;
        private readonly Func<object, string, string, DbCommand, DbCommand> _commandSetupDelegate;
        private readonly Action<object, DbCommand, DbDataReader> _actionOutParameterLoader;
        private readonly CommandBehavior _commandBehavior;
        private readonly int _bufferSize;
        private readonly DbCommandConfiguration _configuration;
        private DbDataReader _reader;

        //object to share in transaction context
        private DbCommand _command;
        private DbConnection _connection;
        private readonly DbTransaction _transaction;

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

        public DatabaseCommand()
        {
        }

        public DatabaseCommand(in DbSettings options)
        {
            _connectionString = options.StringConnection;
            _provider = options.SqlProvider;
            _timeout = options.ConnectionTimeout;
        }

        public DatabaseCommand(
            in DbSettings options,
            in string script,
            in object filter,
            in DbCommandConfiguration configuration,
            in bool buffered,
            in DbTransaction transaction = null,
            in DbCommand command = null)
        {
            _configuration = configuration;
            _bufferSize = options.BufferSize;
            _script = script;
            _connectionString = options.StringConnection;
            _provider = options.SqlProvider;
            _timeout = options.ConnectionTimeout;
            _filter = filter;
            _command = command;

            _operationKey = 17;
            unchecked
            {
                _operationKey = (_operationKey * 23) + _script.GetHashCode();
                _operationKey = (_operationKey * 23) + _provider.GetHashCode();
                _operationKey = (_operationKey * 23) + configuration.GetHashCode();
            }

            if (transaction != null)
            {
                _transaction = transaction;
                _connection = _command.Connection;
                _command.Parameters.Clear();

                unchecked
                {
                    _operationKey = (_operationKey * 23) + _transaction.GetType().GetHashCode();
                }
            }

            Type filterType = null;
            if (_filter != null)
            {
                filterType = filter.GetType();
                _preparationQueryKey = (_operationKey * 23) + filterType.GetHashCode();
            }
            else
            {
                _preparationQueryKey = _operationKey;
            }

            if (DatabaseHelperProvider.CommandMetadata.TryGetValue(_preparationQueryKey, out var commandMetadata))
            {
                _commandType = commandMetadata.CommandType;
                _commandBehavior = commandMetadata.CommandBehavior;
                _commandSetupDelegate = commandMetadata.LoadParametersDelegate;
                _actionOutParameterLoader = commandMetadata.LoadOutParametersDelegate;
            }
            else
            {
                _commandBehavior = configuration.CommandBehavior;

                var isStoreProcedure = QueryValidators.IsStoredProcedure(script);
                _commandType = isStoreProcedure ? CommandType.StoredProcedure : CommandType.Text;

                DbParameterInfo[] additionalOutputParameters = null;

                if (_provider == SqlProvider.Oracle && isStoreProcedure && configuration.EligibleForAddOracleCursors())
                {
                    int cursorsToAdd = new [] { MethodHandled.ToSingleQueryString, MethodHandled.ToListQueryString }.Contains(configuration.MethodHandled) ? 1 : (int)configuration.MethodHandled - 3;
                    additionalOutputParameters = new DbParameterInfo[cursorsToAdd];

                    for (int i = 0; i < cursorsToAdd; i++)
                    {
                        string name = $"C_CURSOR{i + 1}";
                        additionalOutputParameters[i] = new DbParameterInfo(in name, in name, 0, 0, ParameterDirection.Output, null, null, 121, null); // 121 -> RefCursor
                    }
                }

                var loaderConfiguration = new LoaderConfiguration(
                    keyAsReturnValue: in configuration.KeyAsReturnValue,
                    generateParameterWithKeys: in configuration.GenerateParameterWithKeys,
                    additionalOracleRefCursors: additionalOutputParameters?.ToList(),
                    provider: in _provider,
                    fetchSize: options.FetchSize,
                    isExecuteNonQuery: configuration.MethodHandled == MethodHandled.Execute,
                    query: in _script,
                    timeout: options.ConnectionTimeout,
                    commandType: in _commandType,
                    isTransactionOperation: transaction != null,
                    prepareStatements: options.PrepareStatements);

                (_commandSetupDelegate, _actionOutParameterLoader) = DatabaseProvider.GetCommandMetaData(in loaderConfiguration, in _preparationQueryKey, in _commandType, in filterType, in buffered, ref _commandBehavior);
            }

            if (options.DetailErrorMessage)
                EnsureRequest();
        }

        void EnsureRequest()
        {
            if (string.IsNullOrEmpty(_script))
                throw new InvalidOperationException("The script was no provided");

            //unsupported operations
            if (_commandType == CommandType.StoredProcedure && _provider == SqlProvider.Sqlite)
                throw new InvalidOperationException("SQLite does not support stored procedures");

            //validate parameters
            if (_filter == null && QueryValidators.IsDML(_script) && QueryValidators.ScriptExpectParameterMatch(_script))
                throw new InvalidOperationException("DML script expects parameters, but no parameters were provided");

            if (_filter != null && QueryValidators.IsDDL(_script))
                throw new InvalidOperationException("DDL scripts does not support parameters");

            if (_filter != null && QueryValidators.IsDCL(_script))
                throw new InvalidOperationException("DCL scripts does not support parameters");

            if (_filter != null && QueryValidators.IsAnonymousBlock(_script))
                throw new InvalidOperationException("Anonymous block does not support parameters");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal DbCommand CreateEmptyCommand() => _connection.CreateCommand();

        internal void OpenConnection()
        {
            _connection = DatabaseProvider.CreateConnection(in _provider, in _connectionString);
            _connection.Open();
        }

        internal DbTransaction BeginTransaction()
        {
            _connection = DatabaseProvider.CreateConnection(in _provider, in _connectionString);
            _connection.Open();
            return _connection.BeginTransaction();
        }

        internal async Task<DbTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        {
            _connection = DatabaseProvider.CreateConnection(in _provider, in _connectionString);
            await _connection.OpenAsync(cancellationToken);
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            return await _connection.BeginTransactionAsync(cancellationToken);
#else
            return _connection.BeginTransaction();
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Prepare()
        {
            if (_connection == null)
            {
                _command = _commandSetupDelegate(_filter, _connectionString, _script, null);
                _connection = _command.Connection;
                _connection.Open();
            }
            else
            {
                _command.CommandText = _script;
                _command.CommandType = _commandType;
                _command.Transaction = _transaction;
                _command.CommandTimeout = _timeout;
                _commandSetupDelegate(_filter, null, null, _command);
            }
        }

        /// <summary>
        /// Prepare the command to be executed, opening the connection and load the parameters
        /// </summary>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>Parameters of the command</returns>
        public async Task PrepareAsync(CancellationToken cancellationToken)
        {
            if (_connection == null)
            {
                _command = _commandSetupDelegate(_filter, _connectionString, _script, null);
                _connection = _command.Connection;
                await _connection.OpenAsync(cancellationToken);
            }
            else
            {
                _command.CommandText = _script;
                _command.CommandType = _commandType;
                _command.Transaction = _transaction;
                _command.CommandTimeout = _timeout;
                _commandSetupDelegate(_filter, null, null, _command);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object ExecuteScalar() => _command.ExecuteScalar();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ExecuteNonQuery() => _command.ExecuteNonQuery();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => await _command.ExecuteNonQueryAsync(cancellationToken);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NextResult() => _reader.NextResult();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValuesOutFields()
        {
            if (_actionOutParameterLoader != null)
                _actionOutParameterLoader(_filter, _command, _reader);
        }

        #region reader operations
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<List<T>> ReadListItemsAsync<T>(CancellationToken cancellationToken)
        {
            _reader = await _command.ExecuteReaderAsync(_commandBehavior, cancellationToken);

            var parser = GetParserTypeDelegate<T>(in _reader, in _operationKey, in _preparationQueryKey, in _provider, in _bufferSize);

            var list = new List<T>();

            while (await _reader.ReadAsync(cancellationToken))
                list.Add(parser(_reader));

            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<List<T>> ReadListNextItemsAsync<T>(CancellationToken cancellationToken)
        {
            if (await _reader.NextResultAsync(cancellationToken))
            {
                var parser = GetParserTypeDelegate<T>(in _reader, in _operationKey, in _preparationQueryKey, in _provider, in _bufferSize);

                var list = new List<T>();
                while (await _reader.ReadAsync(cancellationToken))
                    list.Add(parser(_reader));

                return list;
            }

            return Enumerable.Empty<T>().ToList();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<T> ReadListItems<T>()
        {
            _reader = _command!.ExecuteReader(_commandBehavior);

            var parser = GetParserTypeDelegate<T>(in _reader, in _operationKey, in _preparationQueryKey, in _provider, in _bufferSize);

            var list = new List<T>();

            while (_reader.Read())
                list.Add(parser(_reader));

            return list;
        }

        public IEnumerable<List<T>> FetchData<T>(int batchSize)
        {
            _reader = _command!.ExecuteReader(_commandBehavior);

            var parser = GetParserTypeDelegate<T>(in _reader, in _operationKey, in _preparationQueryKey, in _provider, in _bufferSize);

            int counter = 0;

            var list = new List<T>(batchSize);

            while (_reader.Read())
            {
                counter++;
                list.Add(parser(_reader));

                if (counter == batchSize)
                {
                    yield return list;
                    list.Clear();
                    counter = 0;
                }
            }

            if (counter > 0)
                yield return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<T> ReadListNextItems<T>()
        {
            if (_reader.NextResult())
            {
                var parser = GetParserTypeDelegate<T>(in _reader, in _operationKey, in _preparationQueryKey, in _provider, in _bufferSize);

                var list = new List<T>();

                while (_reader.Read())
                    list.Add(parser(_reader));

                return list;
            }

            return Enumerable.Empty<T>().ToList();
        }

        #endregion reader operations

        public void Dispose()
        {
            if (_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }

            if (_transaction == null)
            {
                if (_command != null)
                    _command?.Dispose();

                _connection?.Dispose();
            }
        }

        public async ValueTask DisposeAsync()
        {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            if (_reader != null)
            {
                await _reader.DisposeAsync();
                _reader = null;
            }

            if (_transaction == null)
            {
                if (_command != null)
                    await _command.DisposeAsync();

                if (_connection != null)
                    await _connection.DisposeAsync();
            }
#else
            await Task.Run(() => Dispose());
#endif
        }

    }
}
