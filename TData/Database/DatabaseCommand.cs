using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using TData.Core.Provider;
using TData.Configuration;
using TData.Database;
using TData.DbResult;
using static TData.Core.Provider.DatabaseProvider;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TData.Tests")]
namespace TData
{
    internal partial class DatabaseCommand : IDisposable
#if NET5_0_OR_GREATER
          , IAsyncDisposable
#endif
    {
        private readonly CommandType _commandType;
        private readonly string _script;
        private readonly string _connectionString;
        private readonly SqlProvider _provider;
        private readonly int _timeout;
        private readonly object _filter;
        private readonly object[] _values;
        private readonly int _preparationQueryKey;
        private readonly ConfigureCommandDelegate _commandSetupDelegate;
        private readonly ConfigureCommandDelegate2 _commandSetupDelegate2;
        private readonly Action<object, DbCommand, DbDataReader> _actionOutParameterLoader;
        private readonly CommandBehavior _commandBehavior;
        private readonly int _bufferSize;
        private readonly DbCommandConfiguration _configuration;
        
        //object to share in transaction context
        private DbDataReader _reader;
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
            in DbCommand command = null,
            in bool addPagingParams = false)
        {
            _configuration = configuration;
            _bufferSize = options.BufferSize;
            _script = script;
            _connectionString = options.StringConnection;
            _provider = options.SqlProvider;
            _timeout = options.ConnectionTimeout;
            _filter = filter;
            _command = command;

            Type filterType = null;
            bool hasFilter = false;
            bool isTransaction = false;
            var operationKey = 17;

            if (transaction != null)
            {
                isTransaction = true;
                _transaction = transaction;
                _connection = _command.Connection;
                _command.Parameters.Clear();
                operationKey = (operationKey * 23) + _transaction.GetType().GetHashCode();
            }

            unchecked
            {
                if (filter != null)
                {
                    hasFilter = true;
                    filterType = filter.GetType();
                    operationKey = (operationKey * 23) + filterType.GetHashCode();
                }

                operationKey = (operationKey * 23) + _script.GetHashCode();
                operationKey = (operationKey * 23) + (int)_provider;
                operationKey = (operationKey * 23) + configuration.GetHashCode();
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
                bool isExecuteNonQuery = configuration.IsExecuteNonQuery();

                bool expectBindValueParameters;
                bool canPrepareStatement = false;
                string transformedScript = null;
                LoaderConfiguration commandConfiguration;

                if (isStoreProcedure && _provider == SqlProvider.PostgreSql)
                {
                    _commandType = CommandType.Text;
                    isStoreProcedure = false;

                    commandConfiguration = GetCommandConfiguration(in options, in _commandType, in configuration, in isTransaction, in isStoreProcedure, true);

                    DbParameterInfo[] localParameters = null;
                    (_commandSetupDelegate, _actionOutParameterLoader) = GetCommandMetaData(in commandConfiguration, in isExecuteNonQuery, in filterType, in addPagingParams, ref localParameters);

                    bool existsOutParameter = false;
                    if (localParameters != null)
                    {
                        foreach (var item in localParameters)
                        {
                            if (item.IsOutput)
                            {
                                existsOutParameter = true;
                                break;
                            }
                        }
                    }

                    if (!isExecuteNonQuery && existsOutParameter)
                        throw new PostgreSQLInvalidRequestCallException();

                    transformedScript = _script = TransformPostgresScript(in script, in localParameters, in isExecuteNonQuery);
                }
                else
                {
                    if (isStoreProcedure)
                    {
                        expectBindValueParameters = false;
                        _commandType = CommandType.StoredProcedure;
                    }
                    else
                    {
                        ValidateScript(in script, in options.SqlProvider, in isExecuteNonQuery, in hasFilter, out var isDML, out expectBindValueParameters, in addPagingParams);
                        canPrepareStatement = isDML && expectBindValueParameters;
                        _commandType = CommandType.Text;
                    }

                    commandConfiguration = GetCommandConfiguration(in options, in _commandType, in configuration, in isTransaction, in isStoreProcedure, in canPrepareStatement);
                    DbParameterInfo[] localParameters = null;
                    (_commandSetupDelegate, _actionOutParameterLoader) = GetCommandMetaData(in commandConfiguration, in isExecuteNonQuery, in filterType, in addPagingParams, ref localParameters);
                }

                if (buffered)
                    DatabaseHelperProvider.CommandMetadata.TryAdd(_preparationQueryKey, new CommandMetaData(in _commandSetupDelegate, null, in _actionOutParameterLoader, in configuration.CommandBehavior, in _commandType, in transformedScript, in commandConfiguration.ShouldIncludeSequentialBehavior));
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

            bool isTransaction = false;
            var operationKey = 17;

            if (transaction != null)
            {
                isTransaction = true;
                _transaction = transaction;
                _connection = _command.Connection;
                _command.Parameters.Clear();
                operationKey = (operationKey * 23) + _transaction.GetType().GetHashCode();
            }

            unchecked
            {
                operationKey = (operationKey * 23) + _script.GetHashCode();
                operationKey = (operationKey * 23) + _provider.GetHashCode();
                operationKey = (operationKey * 23) + configuration.GetHashCode();
            }

            _preparationQueryKey = operationKey;
            DbParameterInfo[] convertedParameters = Array.Empty<DbParameterInfo>();
            if (parameters is DbParameterInfo[] && parameters.Length > 0)
            {
                convertedParameters = (DbParameterInfo[])parameters;
                _values = new object[convertedParameters.Length];
                for (int i = 0; i < convertedParameters.Length; i++)
                {
                    _values[i] = convertedParameters[i].Value;
                }
            }
            else
            {
                _values = parameters;
            }

            if (DatabaseHelperProvider.CommandMetadata.TryGetValue(_preparationQueryKey, out var commandMetadata))
            {
                _commandSetupDelegate2 = commandMetadata.LoadParametersDelegate2;
                _actionOutParameterLoader = commandMetadata.LoadOutParametersDelegate;
            }
            else
            {
                var commandConfiguration = GetCommandConfiguration(in options, in _commandType, in configuration, isTransaction, false, true);
                _commandSetupDelegate2 = DatabaseProvider.GetCommandMetaData2(in commandConfiguration, convertedParameters);

                if (buffered)
                    DatabaseHelperProvider.CommandMetadata.TryAdd(_preparationQueryKey, new CommandMetaData(null, in _commandSetupDelegate2, null, in configuration.CommandBehavior, in _commandType, null, in commandConfiguration.ShouldIncludeSequentialBehavior));
            }
        }

        private static LoaderConfiguration GetCommandConfiguration(in DbSettings options, in CommandType commandType, in DbCommandConfiguration configuration, in bool isTransaction, in bool isStoreProcedure, in bool canPrepareStatement)
        {
            int cursorsToAdd = 0;

            if (options.SqlProvider == SqlProvider.Oracle && isStoreProcedure && configuration.EligibleForAddOracleCursors())
            {
                cursorsToAdd = MethodHandled.FetchOneQueryString == configuration.MethodHandled || configuration.MethodHandled == MethodHandled.FetchListQueryString ? 1 :
                               configuration.MethodHandled == MethodHandled.FetchTupleQueryString_2 ? 2 :
                               configuration.MethodHandled == MethodHandled.FetchTupleQueryString_3 ? 3 :
                               configuration.MethodHandled == MethodHandled.FetchTupleQueryString_4 ? 4 :
                               configuration.MethodHandled == MethodHandled.FetchTupleQueryString_5 ? 5 :
                               configuration.MethodHandled == MethodHandled.FetchTupleQueryString_6 ? 6 :
                               configuration.MethodHandled == MethodHandled.FetchTupleQueryString_7 ? 7 : 1 ;
            }

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
                canPrepareStatement: canPrepareStatement,
                shouldIncludeSequentialBehavior: !configuration.IsExecuteNonQuery());
        }

        private static void ValidateScript(in string script, in SqlProvider provider, in bool isExecuteNonQuery, in bool hasFilter, out bool isDML, out bool expectBindValueParameters, in bool addPagingParams = false)
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
            else if (isDML && expectBindValueParameters && !addPagingParams)
            {
                throw new MissingParametersException();
            }

            if ((isDCL || isDDL) && !isExecuteNonQuery)
                throw new NotSupportedCommandTypeException();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal DbCommand CreateEmptyCommand() => _connection.CreateCommand();

        internal void OpenConnection()
        {
            _connection = CreateConnection(in _provider, in _connectionString);
            _connection.Open();
        }

        internal async Task OpenConnectionAsync(CancellationToken cancellationToken)
        {
            _connection = CreateConnection(in _provider, in _connectionString);
            await _connection.OpenAsync(cancellationToken);
        }

        internal DbTransaction BeginTransaction()
        {
            _connection = CreateConnection(in _provider, in _connectionString);
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

        internal void SetPaging(in int newOffset, in int pageSize, in string bindVariableSymbol)
        {
            _command.Parameters[$"{bindVariableSymbol}{DatabaseHelperProvider.OFFSET_PARAMETER}"].Value = newOffset;
            _command.Parameters[$"{bindVariableSymbol}{DatabaseHelperProvider.PAGESIZE_PARAMETER}"].Value = pageSize;
        }

        internal void Prepare()
        {
            if (_connection == null)
            {
                _command = _commandSetupDelegate(in _filter, in _connectionString, in _script, null);
                _connection = _command.Connection;
            }
            else
            {
                _command.CommandText = _script;
                _command.CommandType = _commandType;
                _command.CommandTimeout = _timeout;
                _ = _commandSetupDelegate(in _filter, null, null, in _command);
            }
        }

        internal void Prepare2()
        {
            if (_connection == null)
            {
                _command = _commandSetupDelegate2(in _values, in _connectionString, in _script, null);
                _connection = _command.Connection;
            }
            else
            {
                _command.CommandText = _script;
                _command.CommandType = _commandType;
                _command.CommandTimeout = _timeout;
                _ = _commandSetupDelegate2(in _values, null, null, in _command);
            }
        }

        public object ExecuteScalar()
        {
            var value = _command.ExecuteScalar();

            if (_actionOutParameterLoader != null)
                _actionOutParameterLoader(_filter, _command, _reader);

            return value;
        }

        public async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            var value = await _command.ExecuteScalarAsync(cancellationToken);

            if (_actionOutParameterLoader != null)
                _actionOutParameterLoader(_filter, _command, _reader);

            return value;
        }

        public int ExecuteNonQuery()
        {
            var rowsAffected = _command.ExecuteNonQuery();

            if (_actionOutParameterLoader != null)
                _actionOutParameterLoader(_filter, _command, _reader);

            return rowsAffected;
        }

        public async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            var rowsAffected = await _command.ExecuteNonQueryAsync(cancellationToken);

            if (_actionOutParameterLoader != null)
                _actionOutParameterLoader(_filter, _command, _reader);

            return rowsAffected;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void SetValuesOutFields()
        {
            if (_actionOutParameterLoader != null)
                _actionOutParameterLoader(_filter, _command, _reader);
        }

        #region reader operations

        public async Task<List<T>> ReadListItemsAsync<T>(CancellationToken cancellationToken, int expectedRows = 0, bool closeOnComplete = false)
        {
            _reader = await _command.ExecuteReaderAsync(_commandBehavior, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                throw new TaskCanceledException();

            var parser = GetParserTypeDelegate<T>(in _reader, in _preparationQueryKey, in _provider, in _bufferSize, in expectedRows);

            var list = new List<T>(expectedRows);

            while (await _reader.ReadAsync(cancellationToken))
                list.Add(parser(in _reader));

            return list;
        }

        public async Task<List<T>> ReadListNextItemsAsync<T>(CancellationToken cancellationToken, int expectedRows = 0)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new TaskCanceledException();

            if (await _reader.NextResultAsync(cancellationToken))
            {
                var parser = GetParserTypeDelegate<T>(in _reader, in _preparationQueryKey, in _provider, in _bufferSize, in expectedRows);

                var list = new List<T>(expectedRows);
                while (await _reader.ReadAsync(cancellationToken))
                    list.Add(parser(in _reader));

                return list;
            }

            return new List<T>(0);
        }

        public List<T> ReadListItems<T>(int expectedRows = 0)
        {
            _reader = _command!.ExecuteReader(_commandBehavior);
            var parser = GetParserTypeDelegate<T>(in _reader, in _preparationQueryKey, in _provider, in _bufferSize, in expectedRows);

            var list = new List<T>(expectedRows);

            while (_reader.Read())
                list.Add(parser(in _reader));

            return list;
        }

        public IEnumerable<List<T>> FetchData<T>(int batchSize)
        {
            _reader = _command!.ExecuteReader(_commandBehavior);

            var parser = GetParserTypeDelegate<T>(in _reader, in _preparationQueryKey, in _provider, in _bufferSize, in batchSize);

            if (_provider == SqlProvider.Oracle && batchSize > 0)
                DatabaseInternalConfiguration.SetFetchSizeOracleReader(_reader, batchSize);

            int counter = 0;

            var list = new List<T>(batchSize);

            while (_reader.Read())
            {
                counter++;
                list.Add(parser(in _reader));

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

        public async IAsyncEnumerable<List<T>> FetchDataAsync<T>(int batchSize, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _reader = await _command!.ExecuteReaderAsync(_commandBehavior, cancellationToken);

            var parser = GetParserTypeDelegate<T>(in _reader, in _preparationQueryKey, in _provider, in _bufferSize, in batchSize);

            if (_provider == SqlProvider.Oracle && batchSize > 0)
                DatabaseInternalConfiguration.SetFetchSizeOracleReader(_reader, batchSize);

            int counter = 0;

            var list = new List<T>(batchSize);

            while (await _reader.ReadAsync(cancellationToken))
            {
                counter++;
                list.Add(parser(in _reader));

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

        public List<T> ReadListNextItems<T>(int expectedRows = 0)
        {
            if (_reader.NextResult())
            {
                var parser = GetParserTypeDelegate<T>(in _reader, in _preparationQueryKey, in _provider, in _bufferSize, in expectedRows);

                var list = new List<T>(expectedRows);

                while (_reader.Read())
                    list.Add(parser(in _reader));

                return list;
            }

            return new List<T>(0);
        }

        public T ProcessReaderToSingle<T>()
        {
            using var reader = _command!.ExecuteReader(_commandBehavior);
            var parser = GetParserTypeDelegate<T>(in reader, in _preparationQueryKey, in _provider, in _bufferSize, 1);

            T item = default;

            while (reader.Read())
            {
                item = parser(in reader);
                break;
            }

            if (_actionOutParameterLoader != null)
                _actionOutParameterLoader(_filter, _command, reader);

            return item;
        }

        public List<T> ProcessReaderToList<T>(int expectedRows = 0)
        {
            _reader = _command!.ExecuteReader(_commandBehavior);
            var parser = GetParserTypeDelegate<T>(in _reader, in _preparationQueryKey, in _provider, in _bufferSize, in expectedRows);

            var list = new List<T>(expectedRows);

            while (_reader.Read())
                list.Add(parser(in _reader));

            if (_actionOutParameterLoader != null)
                _actionOutParameterLoader(_filter, _command, _reader);

            return list;
        }

        public async Task<T> ProcessReaderToSingleAsync<T>(CancellationToken cancellationToken)
        {
            using var reader = await _command!.ExecuteReaderAsync(_commandBehavior, cancellationToken);
            var parser = GetParserTypeDelegate<T>(in reader, in _preparationQueryKey, in _provider, in _bufferSize, 1);

            T item = default;

            while (await reader.ReadAsync(cancellationToken))
            {
                item = parser(in reader);
                break;
            }

            if (_actionOutParameterLoader != null)
                _actionOutParameterLoader(_filter, _command, reader);

            return item;
        }

        public async Task<List<T>> ProcessReaderToListAsync<T>(CancellationToken cancellationToken, int expectedRows = 0)
        {
            using var reader = await _command!.ExecuteReaderAsync(_commandBehavior, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                throw new TaskCanceledException();

            var parser = GetParserTypeDelegate<T>(in reader, in _preparationQueryKey, in _provider, in _bufferSize, in expectedRows);

            var list = new List<T>(expectedRows);

            while (await reader.ReadAsync(cancellationToken))
                list.Add(parser(in reader));

            if (_actionOutParameterLoader != null)
                _actionOutParameterLoader(_filter, _command, reader);

            return list;
        }

        #region pagination

        public IEnumerable<List<T>> ReadBatchList<T>(int offset, int pageSize)
        {
            var bindVariable = _provider == SqlProvider.PostgreSql ? "@" : "";
            SetPaging(in offset, in pageSize, in bindVariable);

            var list = new List<T>(pageSize);

            ParserDelegate<T> parser = null;
            using (var reader = _command.ExecuteReader(_commandBehavior))
            {
                if (_provider == SqlProvider.Oracle)
                    DatabaseInternalConfiguration.SetFetchSizeOracleReader(in reader, in pageSize);

                parser = GetParserTypeDelegate<T>(in reader, in _preparationQueryKey, in _provider, in _bufferSize, in pageSize);

                while (reader.Read())
                    list.Add(parser(in reader));
            }

            yield return list;

            if (list.Count < pageSize)
            {
                Dispose();
                yield break;
            }

            offset += pageSize;

            while (true)
            {
                var batch = new List<T>(pageSize);

                using (var reader = _command!.ExecuteReader(_commandBehavior))
                {
                    if (_provider == SqlProvider.Oracle)
                        DatabaseInternalConfiguration.SetFetchSizeOracleReader(in reader, in pageSize);

                    while (reader.Read())
                        batch.Add(parser(in reader));
                }

                if (batch.Count < pageSize)
                {
                    if (batch.Count > 0)
                    {
                        batch.TrimExcess();
                        yield return batch;
                    }

                    Dispose();
                    yield break;
                }

                yield return batch;

                offset += pageSize;
                SetPaging(in offset, in pageSize, in bindVariable);
            }
        }

        public IEnumerable<List<TDataRow>> ReadBatchListDataRow(int offset, int pageSize)
        {
            var bindVariable = _provider == SqlProvider.PostgreSql ? "@" : "";

            SetPaging(in offset, in pageSize, in bindVariable);

            while (true)
            {
                var newPage = new List<TDataRow>(pageSize);
                using (var reader = _command!.ExecuteReader(_commandBehavior))
                {
                    if (_provider == SqlProvider.Oracle)
                        DatabaseInternalConfiguration.SetFetchSizeOracleReader(in reader, in pageSize);

                    var fieldCount = reader.FieldCount;

                    while (reader.Read())
                    {
                        var data = new object[fieldCount];
                        reader.GetValues(data);
                        newPage.Add(new TDataRow(in data));
                    }
                }

                if (newPage.Count < pageSize)
                {
                    if (newPage.Count > 0)
                    {
                        //important to remove the empty items
                        newPage.TrimExcess();
                        yield return newPage;
                    }

                    Dispose();
                    yield break;
                }

                yield return newPage;

                offset += pageSize;
                SetPaging(in offset, in pageSize, in bindVariable);
            }
        }

        public async IAsyncEnumerable<List<T>> ReadBatchListAsync<T>(int offset, int pageSize, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var bindVariable = _provider == SqlProvider.PostgreSql ? "@" : "";
            SetPaging(offset, pageSize, bindVariable);

            if (cancellationToken.IsCancellationRequested)
            {
                Dispose();
                throw new TaskCanceledException();
            }

            var list = new List<T>(pageSize);
            ParserDelegate<T> parser = null;

            using (var reader = await _command.ExecuteReaderAsync(_commandBehavior, cancellationToken))
            {
                if (_provider == SqlProvider.Oracle)
                    DatabaseInternalConfiguration.SetFetchSizeOracleReader(in reader, pageSize);

                parser = GetParserTypeDelegate<T>(in reader, in _preparationQueryKey, in _provider, in _bufferSize, in pageSize);

                while (await reader.ReadAsync(cancellationToken))
                    list.Add(parser(in reader));
            }

            yield return list;

            if (list.Count < pageSize)
            {
                Dispose();
                yield break;
            }

            offset += pageSize;

            while (true)
            {
                var newPage = new List<T>(pageSize);
                using (var reader = await _command.ExecuteReaderAsync(_commandBehavior, cancellationToken))
                {
                    if (_provider == SqlProvider.Oracle)
                        DatabaseInternalConfiguration.SetFetchSizeOracleReader(in reader, pageSize);

                    while (await reader.ReadAsync(cancellationToken))
                        newPage.Add(parser(in reader));
                }

                if (newPage.Count < pageSize)
                {
                    if (newPage.Count > 0)
                    {
                        //important to remove the empty items
                        newPage.TrimExcess();
                        yield return newPage;
                    }

                    Dispose();
                    yield break;
                }

                offset += pageSize;
                yield return newPage;

                SetPaging(offset, pageSize, bindVariable);
            }
        }


        #endregion

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

            GC.SuppressFinalize(this);
        }

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        public async ValueTask DisposeAsync()
        {
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

            GC.SuppressFinalize(this);
        }
#endif

    }
}
