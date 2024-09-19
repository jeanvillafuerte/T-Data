using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Thomas.Database.Core.Provider;
using Thomas.Database.Configuration;

[assembly: InternalsVisibleTo("Thomas.Database.Tests")]
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
        private readonly object[] _values;
        private readonly int _preparationQueryKey;
        private readonly Func<object, string, string, DbCommand, DbCommand> _commandSetupDelegate;
        private readonly Func<object[], string, string, DbCommand, DbCommand> _commandSetupDelegate2;
        private readonly Action<object, DbCommand, DbDataReader> _actionOutParameterLoader;
        private readonly CommandBehavior _commandBehavior;
        private readonly int _bufferSize;
        private readonly DbCommandConfiguration _configuration;
        private DbDataReader _reader;

        //object to share in transaction context
        internal DbCommand _command;
        private DbConnection _connection;
        private readonly DbTransaction _transaction;

        //output parameters
        internal IEnumerable<IDbDataParameter> OutParameters
        {
            get
            {
                if (_command.Parameters == null || _command.Parameters.Count == 0)
                {
                    yield break;
                }

                foreach (DbParameter item in _command.Parameters)
                {
                    if (item.Direction == ParameterDirection.InputOutput || item.Direction == ParameterDirection.Output || item.Direction == ParameterDirection.ReturnValue)
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
            _script = string.IsNullOrEmpty(script) ? throw new ScriptNotProvidedException() : script;
            _connectionString = options.StringConnection;
            _provider = options.SqlProvider;
            _timeout = options.ConnectionTimeout;
            _filter = filter;
            _command = command;

            bool hasFilter = filter != null;
            bool isTransaction = transaction != null;
            var operationKey = 17;
            unchecked
            {
                operationKey = (operationKey * 23) + _script.GetHashCode();
                operationKey = (operationKey * 23) + _provider.GetHashCode();
                operationKey = (operationKey * 23) + configuration.GetHashCode();
            }

            if (isTransaction)
            {
                _transaction = transaction;
                _connection = _command.Connection;
                _command.Parameters.Clear();

                unchecked
                {
                    operationKey = (operationKey * 23) + _transaction.GetType().GetHashCode();
                }
            }

            Type filterType = null;
            if (hasFilter)
            {
                filterType = filter.GetType();
                _preparationQueryKey = (operationKey * 23) + filterType.GetHashCode();
            }
            else
            {
                _preparationQueryKey = operationKey;
            }

            if (DatabaseHelperProvider.CommandMetadata.TryGetValue(_preparationQueryKey, out var commandMetadata))
            {
                _commandType = commandMetadata.CommandType;
                _commandBehavior = commandMetadata.CommandBehavior;
                _commandSetupDelegate = commandMetadata.LoadParametersDelegate;
                _actionOutParameterLoader = commandMetadata.LoadOutParametersDelegate;
                _script = commandMetadata.TransformedScript ?? _script;
            }
            else
            {
                _commandBehavior = configuration.CommandBehavior;

                var isStoreProcedure = QueryValidators.IsStoredProcedure(script);
                bool isExecuteNonQuery = configuration.MethodHandled == MethodHandled.Execute;

                bool expectBindValueParameters;
                bool canPrepareStatement = false;
                string transformedScript = null;

                if (isStoreProcedure && _provider == SqlProvider.PostgreSql)
                {
                    _commandType = CommandType.Text;
                    isStoreProcedure = false;

                    var commandConfiguration = GetCommandConfiguration(in options, in _commandType, in configuration, in isTransaction, in isStoreProcedure, true);

                    DbParameterInfo[] localParameters = null;
                    (_commandSetupDelegate, _actionOutParameterLoader) = DatabaseProvider.GetCommandMetaData(in commandConfiguration, in isExecuteNonQuery, in filterType, ref localParameters);

                    if (localParameters?.Any(x => x.IsOutput) == true && !isExecuteNonQuery)
                        throw new PostgreSQLInvalidRequestCallException();

                    canPrepareStatement = expectBindValueParameters = localParameters.Any(x => x.IsInput);
                    transformedScript = _script = DatabaseProvider.TransformPostgresScript(in script, in localParameters, in isExecuteNonQuery);
                }
                else
                {
                    if (isStoreProcedure)
                    {
                        if (_provider == SqlProvider.Sqlite)
                            throw new SqLiteStoreProcedureNotSupportedException();

                        expectBindValueParameters = false;
                        _commandType = CommandType.StoredProcedure;
                    }
                    else
                    {
                        ValidateScript(in script, in options.SqlProvider, in isExecuteNonQuery, in hasFilter, out var isDML, out expectBindValueParameters);
                        canPrepareStatement = isDML && expectBindValueParameters;
                        _commandType = CommandType.Text;
                    }

                    var commandConfiguration = GetCommandConfiguration(in options, in _commandType, in configuration, transaction != null, in isStoreProcedure, in canPrepareStatement);
                    DbParameterInfo[] localParameters = null;
                    (_commandSetupDelegate, _actionOutParameterLoader) = DatabaseProvider.GetCommandMetaData(in commandConfiguration, in isExecuteNonQuery, in filterType, ref localParameters);
                }

                if (buffered)
                    DatabaseHelperProvider.CommandMetadata.TryAdd(_preparationQueryKey, new CommandMetaData(in _commandSetupDelegate, null, in _actionOutParameterLoader, in configuration.CommandBehavior, in _commandType, transformedScript));
            }
        }

        public DatabaseCommand(
            in DbSettings options,
            in string script,
            in DbCommandConfiguration configuration,
            in bool buffered,
            in object[] parameters = null,
            in DbTransaction transaction = null,
            in DbCommand command = null)
        {
            _configuration = configuration;
            _bufferSize = options.BufferSize;
            _script = script;
            _connectionString = options.StringConnection;
            _provider = options.SqlProvider;
            _timeout = options.ConnectionTimeout;
            _command = command;
            _commandType = CommandType.Text;
            _commandBehavior = configuration.CommandBehavior;

            bool hasFilter = parameters != null && parameters.Length > 0;
            bool isTransaction = transaction != null;
            var operationKey = 17;
            unchecked
            {
                operationKey = (operationKey * 23) + _script.GetHashCode();
                operationKey = (operationKey * 23) + _provider.GetHashCode();
                operationKey = (operationKey * 23) + configuration.GetHashCode();
            }

            if (isTransaction)
            {
                _transaction = transaction;
                _connection = _command.Connection;
                _command.Parameters.Clear();

                unchecked
                {
                    operationKey = (operationKey * 23) + _transaction.GetType().GetHashCode();
                }
            }

            _preparationQueryKey = operationKey;

            if (DatabaseHelperProvider.CommandMetadata.TryGetValue(_preparationQueryKey, out var commandMetadata))
            {
                if (parameters is DbParameterInfo[] convertedParameters)
                    _values = convertedParameters.Select(x => x.Value).ToArray();
                else
                    _values = parameters;

                _commandSetupDelegate2 = commandMetadata.LoadParametersDelegate2;
                _actionOutParameterLoader = commandMetadata.LoadOutParametersDelegate;
            }
            else
            {
                DbParameterInfo[] convertedParameters = null;
                if (parameters != null && parameters.Length > 0)
                {
                    convertedParameters = (DbParameterInfo[])parameters;
                    _values = convertedParameters.Select(x => x.Value).ToArray();
                }

                var commandConfiguration = GetCommandConfiguration(in options, in _commandType, in configuration, transaction != null, false, true);
                _commandSetupDelegate2 = DatabaseProvider.GetCommandMetaData2(in commandConfiguration, convertedParameters);

                if (buffered)
                    DatabaseHelperProvider.CommandMetadata.TryAdd(_preparationQueryKey, new CommandMetaData(null, in _commandSetupDelegate2, null, in configuration.CommandBehavior, in _commandType, null));
            }
        }

        private static LoaderConfiguration GetCommandConfiguration(in DbSettings options, in CommandType commandType, in DbCommandConfiguration configuration, in bool isTransaction, in bool isStoreProcedure, in bool canPrepareStatement)
        {
            int cursorsToAdd = 0;

            if (options.SqlProvider == SqlProvider.Oracle && isStoreProcedure && configuration.EligibleForAddOracleCursors())
                cursorsToAdd = new[] { MethodHandled.FetchOneQueryString, MethodHandled.FetchListQueryString }.Contains(configuration.MethodHandled) ? 1 : (int)configuration.MethodHandled - 3;

            return new LoaderConfiguration(
                keyAsReturnValue: in configuration.KeyAsReturnValue,
                skipAutoGeneratedColumn: in configuration.SkipAutoGeneratedColumn,
                generateParameterWithKeys: in configuration.GenerateParameterWithKeys,
                additionalOracleRefCursors: cursorsToAdd,
                provider: in options.SqlProvider,
                fetchSize: options.FetchSize,
                timeout: options.ConnectionTimeout,
                commandType: in commandType,
                isTransactionOperation: in isTransaction,
                prepareStatements: options.PrepareStatements,
                canPrepareStatement: canPrepareStatement);
        }

        private static void ValidateScript(in string script, in SqlProvider provider, in bool isExecuteNonQuery, in bool hasFilter, out bool isDML, out bool expectBindValueParameters)
        {
            isDML = QueryValidators.IsDML(script);
            var isDDL = QueryValidators.IsDDL(script);
            var isDCL = QueryValidators.IsDCL(script);
            var isAnonymousBlock = QueryValidators.IsAnonymousBlock(script);
            expectBindValueParameters = QueryValidators.ScriptExpectParameterMatch(script);

            if (hasFilter)
            {
                if (isDML && !expectBindValueParameters)
                    throw new NotAllowParametersException();

                if (isDCL || isDDL)
                    throw new UnsupportedParametersException();

                if (isAnonymousBlock && provider != SqlProvider.Oracle)
                    throw new UnsupportedParametersException();
            }
            else if (isDML && expectBindValueParameters)
            {
                throw new MissingParametersException();
            }

            if ((isDCL || isDDL) && !isExecuteNonQuery)
                throw new NotSupportedCommandTypeException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal DbCommand CreateEmptyCommand() => _connection.CreateCommand();

        internal void OpenConnection()
        {
            _connection = DatabaseProvider.CreateConnection(in _provider, in _connectionString);
            _connection.Open();
        }

        internal async Task OpenConnectionAsync(CancellationToken cancellationToken)
        {
            _connection = DatabaseProvider.CreateConnection(in _provider, in _connectionString);
            await _connection.OpenAsync(cancellationToken);
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

        internal void Prepare()
        {
            if (_connection == null)
            {
                _command = _commandSetupDelegate(_filter, _connectionString, _script, null);
                _connection = _command.Connection;
            }
            else
            {
                _command.CommandText = _script;
                _command.CommandType = _commandType;
                _command.CommandTimeout = _timeout;
                _ = _commandSetupDelegate(_filter, null, null, _command);
            }
        }

        internal void Prepare2()
        {
            if (_connection == null)
            {
                _command = _commandSetupDelegate2(_values, _connectionString, _script, null);
                _connection = _command.Connection;
            }
            else
            {
                _command.CommandText = _script;
                _command.CommandType = _commandType;
                _command.CommandTimeout = _timeout;
                _ = _commandSetupDelegate2(_values, null, null, _command);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object ExecuteScalar() => _command.ExecuteScalar();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken) => await _command.ExecuteScalarAsync(cancellationToken);

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
        public async Task<List<T>> ReadListItemsAsync<T>(CancellationToken cancellationToken)
        {
            _reader = await _command.ExecuteReaderAsync(_commandBehavior, cancellationToken);

            var parser = GetParserTypeDelegate<T>(in _reader, in _preparationQueryKey, in _provider, in _bufferSize);

            var list = new List<T>();

            while (await _reader.ReadAsync(cancellationToken))
                list.Add(parser(_reader));

            return list;
        }

        public async Task<List<T>> ReadListNextItemsAsync<T>(CancellationToken cancellationToken)
        {
            if (await _reader.NextResultAsync(cancellationToken))
            {
                var parser = GetParserTypeDelegate<T>(in _reader, in _preparationQueryKey, in _provider, in _bufferSize);

                var list = new List<T>();
                while (await _reader.ReadAsync(cancellationToken))
                    list.Add(parser(_reader));

                return list;
            }

            return Enumerable.Empty<T>().ToList();
        }

        public List<T> ReadListItems<T>()
        {
            _reader = _command!.ExecuteReader(_commandBehavior);

            var parser = GetParserTypeDelegate<T>(in _reader, in _preparationQueryKey, in _provider, in _bufferSize);

            var list = new List<T>();

            while (_reader.Read())
                list.Add(parser(_reader));

            return list;
        }

        public IEnumerable<List<T>> FetchData<T>(int batchSize)
        {
            _reader = _command!.ExecuteReader(_commandBehavior);

            var parser = GetParserTypeDelegate<T>(in _reader, in _preparationQueryKey, in _provider, in _bufferSize, in batchSize);

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

        public async IAsyncEnumerable<List<T>> FetchDataAsync<T>(int batchSize, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _reader = await _command!.ExecuteReaderAsync(_commandBehavior, cancellationToken);

            var parser = GetParserTypeDelegate<T>(in _reader, in _preparationQueryKey, in _provider, in _bufferSize, in batchSize);

            int counter = 0;

            var list = new List<T>(batchSize);

            while (await _reader.ReadAsync(cancellationToken))
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

        public List<T> ReadListNextItems<T>()
        {
            if (_reader.NextResult())
            {
                var parser = GetParserTypeDelegate<T>(in _reader, in _preparationQueryKey, in _provider, in _bufferSize);

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
