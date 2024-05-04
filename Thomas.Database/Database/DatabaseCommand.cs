using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Thomas.Database.Core.Provider;

namespace Thomas.Database
{
    internal partial class DatabaseCommand : IDisposable, IAsyncDisposable
    {
        private readonly CommandType _commandType;
        private readonly string _script;
        private readonly string _connectionString;
        private readonly SqlProvider _provider;
        private readonly int _timeout;
        private readonly object? _filter;
        private readonly ulong _preparationQueryKey;
        private readonly ulong _operationKey;
        private readonly Action<object, DbCommand> _actionParameterLoader;
        private readonly bool _hasOutParameters;
        private readonly CommandBehavior _commandBehavior;
        private DbCommand? _command;
        private readonly bool _excludeAutogenerateColumns;
        private readonly int _bufferSize;
        private readonly bool _prepareStatements;
        private DbDataReader? _reader;

        //object to share in transaction context
        private DbConnection? _connection;
        private readonly DbTransaction? _transaction;

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
            _connectionString = string.Intern(options.StringConnection);
            _provider = options.SqlProvider;
            _timeout = options.ConnectionTimeout;
        }

        public DatabaseCommand(
            in DbSettings options, 
            in string script, 
            in object? filter, 
            in CommandBehavior commandBehavior = CommandBehavior.Default,
            in bool isTupleResult = false, 
            in DbTransaction transaction = null, 
            in DbCommand command = null, 
            in bool excludeAutogenerateColumns = false,
            in bool generateParameterWithKeys = false,
            in bool noCacheMetadata = false)
        {
            _bufferSize = options.BufferSize;
            _script = script;
            _connectionString = options.StringConnection;
            _provider = options.SqlProvider;
            _timeout = options.ConnectionTimeout;
            _prepareStatements = options.PrepareStatements;
            _filter = filter;
            _command = command;
            _excludeAutogenerateColumns = excludeAutogenerateColumns;

            if (transaction != null)
            {
                _transaction = transaction;
                _connection = _command.Connection;
                _command.Parameters.Clear();
            }

            _operationKey = HashHelper.GenerateHash(_script + options.SqlProvider.ToString());

            Type? filterType = null;
            if (_filter == null)
            {
                _preparationQueryKey = _operationKey;
            }
            else
            {
                filterType = _filter.GetType();
                var fullKeyname = _script + options.SqlProvider.ToString() + filterType.FullName!;
                _preparationQueryKey = HashHelper.GenerateHash(fullKeyname);
            }

            string[] filters = Array.Empty<string>();
            if (!noCacheMetadata && DatabaseHelperProvider.CommandMetadata.TryGetValue(_preparationQueryKey, out var commandMetadata))
            {
                _commandType = commandMetadata.CommandType;
                _hasOutParameters = commandMetadata.HasOutputParameters;
                _commandBehavior = commandMetadata.CommandBehavior;
                _actionParameterLoader = commandMetadata.ParserDelegate;
            }
            else
            {
                _commandBehavior = commandBehavior;
                _commandType = QueryValidators.IsStoredProcedure(script) ? CommandType.StoredProcedure : CommandType.Text;
                _actionParameterLoader = DatabaseProvider.GetCommandMetadata(in options, in _preparationQueryKey, in _commandType, filterType, in noCacheMetadata, in _excludeAutogenerateColumns, in generateParameterWithKeys, transaction == null && !isTupleResult, ref _hasOutParameters, ref _commandBehavior, ref filters);
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

        internal DbTransaction BeginTransaction()
        {
            _connection = DatabaseProvider.CreateConnection(in _provider, in _connectionString);
            _connection.Open();
            return _connection.BeginTransaction();
        }

        internal async Task<DbTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        {
            _connection = DatabaseProvider.CreateConnection(in _provider, in _connectionString);
            _connection.OpenAsync(cancellationToken);
            return await _connection.BeginTransactionAsync(cancellationToken);
        }

        //TODO: add support for other providers
        internal void AddOutputParameter(DbParameterInfo parameter)
        {
            int paramIndex = 0;
            switch (_provider)
            {
                case SqlProvider.SqlServer:
                    if (DatabaseHelperProvider.DbTypes(SqlProvider.SqlServer).TryGetValue(parameter.PropertyType, out var enumSqlTextValue) && Enum.TryParse(DatabaseHelperProvider.SqlDbType, enumSqlTextValue, true, out var enumSqlVal))
                    {
                        paramIndex = _command.Parameters.Add(Activator.CreateInstance(DatabaseHelperProvider.SqlDbParameterType, new object[] { parameter.Name, enumSqlVal }));
                    }
                    break;
                case SqlProvider.Oracle:
                    if (DatabaseHelperProvider.DbTypes(SqlProvider.Oracle).TryGetValue(parameter.PropertyType, out var enumOracleTextValue) && Enum.TryParse(DatabaseHelperProvider.OracleDbType, enumOracleTextValue, true, out var enumOracleValue))
                    {
                        paramIndex = _command.Parameters.Add(Activator.CreateInstance(DatabaseHelperProvider.OracleDbParameterType, new object[] { parameter.Name, enumOracleValue }));
                    }
                    break;

            }

            _command.Parameters[paramIndex].Direction = ParameterDirection.Output;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Prepare()
        {
            if (_connection == null)
            {
                _connection = DatabaseProvider.CreateConnection(in _provider, in _connectionString);
                _command = DatabaseProvider.CreateCommand(in _connection, in _script, in _commandType, in _timeout);
                _connection.Open();
            }
            else
            {
                _command.CommandText = _script;
                _command.CommandType = _commandType;
                _command.Transaction = _transaction;
                _command.CommandTimeout = _timeout;
            }

            _actionParameterLoader?.Invoke(_filter, _command);

            if (_commandType == CommandType.Text && _prepareStatements)
                _command.Prepare();
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
                _connection = DatabaseProvider.CreateConnection(in _provider, in _connectionString);
                _command = DatabaseProvider.CreateCommand(in _connection, in _script, in _commandType, in _timeout);
                await _connection.OpenAsync(cancellationToken);
            }
            else
            {
                _command.CommandText = _script;
                _command.CommandType = _commandType;
                _command.Transaction = _transaction;
                _command.CommandTimeout = _timeout;
            }

            _actionParameterLoader?.Invoke(_filter, _command);

            if (_commandType == CommandType.Text && _prepareStatements)
                await _command.PrepareAsync(cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object? ExecuteScalar() => _command.ExecuteScalar();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ExecuteNonQuery() => _command.ExecuteNonQuery();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => await _command.ExecuteNonQueryAsync(cancellationToken);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NextResult() => _reader.NextResult();

        public void SetValuesOutFields()
        {
            if (_hasOutParameters)
            {
                if (_reader != null)
                    NextResult();

                ReadOnlySpan<IDbDataParameter> parameters = OutParameters.ToArray();

                foreach (var item in parameters)
                    item.Value = _command!.Parameters[item.ParameterName].Value;

                foreach (var parameter in parameters)
                {
                    var value = parameter.Value;
                    var property = _filter!.GetType().GetProperty(parameter.ParameterName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                    property?.SetValue(_filter, value);
                }
            }
        }

        #region reader operations
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<List<T>> ReadListItemsAsync<T>(CancellationToken cancellationToken) where T : class, new()
        {
            var list = new List<T>();

            _reader = await _command.ExecuteReaderAsync(_commandBehavior | CommandBehavior.SequentialAccess, cancellationToken);

            var parser = GetParserTypeDelegate<T>(in _reader, in _operationKey, in _bufferSize);

            while (await _reader.ReadAsync(cancellationToken))
                list.Add(parser(_reader));

            parser = null;
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<List<T>> ReadListNextItemsAsync<T>(CancellationToken cancellationToken) where T : class, new()
        {
            var list = new List<T>();

            await _reader.NextResultAsync(cancellationToken);
            var parser = GetParserTypeDelegate<T>(in _reader, in _operationKey, in _bufferSize);

            while (await _reader.ReadAsync(cancellationToken))
                list.Add(parser(_reader));

            parser = null;
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<T> ReadListItems<T>() where T : class, new()
        {
            var list = new List<T>();

            _reader = _command.ExecuteReader(_commandBehavior | CommandBehavior.SequentialAccess);

            var parser = GetParserTypeDelegate<T>(in _reader, in _operationKey, in _bufferSize);

            while (_reader.Read())
                list.Add(parser(_reader));

            parser = null;
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<T> ReadListNextItems<T>() where T : class, new()
        {
            var list = new List<T>();

            _reader.NextResult();
            var parser = GetParserTypeDelegate<T>(in _reader, in _operationKey, in _bufferSize);

            while (_reader.Read())
                list.Add(parser(_reader));

            parser = null;
            return list;
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
    }
}
