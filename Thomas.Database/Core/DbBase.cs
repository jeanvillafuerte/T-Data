using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Thomas.Database.Cache;
using Thomas.Database.Core.Converters;
using Thomas.Database.Core.Provider;
using Thomas.Database.Core.QueryGenerator;
using Thomas.Database.Configuration;

[assembly: InternalsVisibleTo("Thomas.Cache")]
[assembly: InternalsVisibleTo("Thomas.Database.Tests")]

namespace Thomas.Database
{
    public sealed class DbBase : IDatabase
    {

        internal readonly DbSettings Options;
        private readonly ISqlFormatter Formatter;
        private DbTransaction _transaction;
        private DbCommand _command;
        private bool _transactionCompleted;
        private readonly bool _buffered;

        internal DbBase(in DbSettings options, in bool buffered)
        {
            Options = options;
            Formatter = Options.SQLValues;
            _buffered = buffered;
        }

        #region Block

        public void ExecuteBlock(Action<IDatabase> func)
        {
            using var command = new DatabaseCommand(in Options);
            command.OpenConnection();
            _command = command.CreateEmptyCommand();
            func(this);
        }

        public async Task ExecuteBlockAsync(Func<IDatabase, Task> func)
        {
            await using var command = new DatabaseCommand(in Options);
            await command.OpenConnectionAsync(CancellationToken.None);
            _command = command.CreateEmptyCommand();
            await func(this);
        }

        public async Task ExecuteBlockAsync(Func<IDatabase, CancellationToken, Task> func, CancellationToken cancellationToken)
        {
            await using var command = new DatabaseCommand(in Options);
            await command.OpenConnectionAsync(CancellationToken.None);
            _command = command.CreateEmptyCommand();
            await func(this, cancellationToken);
        }

        #endregion Block

        #region transaction
        public T ExecuteTransaction<T>(Func<IDatabase, T> func)
        {
            using var command = new DatabaseCommand(in Options);
            _transaction = command.BeginTransaction();
            _command = command.CreateEmptyCommand();
            _command.Transaction = _transaction;

            try
            {
                var result = func(this);
                _transaction.Commit();
                return result;
            }
            catch (Exception)
            {
                if (!_transactionCompleted)
                {
                    _transaction.Rollback();
                    throw;
                }

                return default;
            }
            finally
            {
                _transaction.Dispose();

                if (_command.Connection?.State == ConnectionState.Open)
                    _command.Connection.Dispose();

                _command.Dispose();

                _transaction = null;
                _command = null;
            }
        }

        public bool ExecuteTransaction(Func<IDatabase, TransactionResult> func)
        {
            using var command = new DatabaseCommand(in Options);
            _transaction = command.BeginTransaction();
            _command = command.CreateEmptyCommand();
            _command.Transaction = _transaction;

            try
            {
                var result = func(this);
                return result == TransactionResult.Committed;
            }
            catch (Exception)
            {
                if (!_transactionCompleted)
                {
                    _transaction.Rollback();
                    throw;
                }

                return false;
            }
            finally
            {
                _transaction.Dispose();

                if (_command.Connection?.State == ConnectionState.Open)
                    _command.Connection.Dispose();

                _command.Dispose();

                _transaction = null;
                _command = null;
            }
        }

        public TransactionResult Commit()
        {
            if (_transaction == null)
                throw new InvalidOperationException("Transaction not started");

            _transactionCompleted = true;
            _transaction.Commit();
            return TransactionResult.Committed;
        }

        public TransactionResult Rollback()
        {
            if (_transaction == null)
                throw new InvalidOperationException("Transaction not started");

            _transactionCompleted = true;
            _transaction.Rollback();
            return TransactionResult.Rollbacked;
        }

        public async Task<T> ExecuteTransactionAsync<T>(Func<IDatabase, Task<T>> func)
        {
            await using var command = new DatabaseCommand(in Options);
            try
            {
                _transaction = await command.BeginTransactionAsync(CancellationToken.None);
                _command = command.CreateEmptyCommand();
                _command.Transaction = _transaction;

                var result = await func(this);
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                await _transaction.CommitAsync();
#else
                _transaction.Commit();
#endif
                return result;
            }
            catch (Exception)
            {
                if (!_transactionCompleted && _transaction?.Connection != null)
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    await _transaction.RollbackAsync();
#else
                    _transaction.Rollback();
#endif
                throw;
            }
            finally
            {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                if (_transaction != null)
                    await _transaction.DisposeAsync();

                if (_command?.Connection?.State == ConnectionState.Open)
                    await _command.Connection.DisposeAsync();

                if (_command != null)
                    await _command.DisposeAsync();
#else
                if (_transaction != null)
                    _transaction.Dispose();

                if (_command?.Connection?.State == ConnectionState.Open)
                    _command.Connection.Dispose();

                if (_command != null)
                    _command.Dispose();
#endif
                _transaction = null;
                _command = null;
            }
        }

        public async Task<T> ExecuteTransactionAsync<T>(Func<IDatabase, CancellationToken, Task<T>> func, CancellationToken cancellationToken)
        {
            await using var command = new DatabaseCommand(in Options);
            try
            {
                _transaction = await command.BeginTransactionAsync(cancellationToken);
                _command = command.CreateEmptyCommand();
                _command.Transaction = _transaction;

                var result = await func(this, cancellationToken);
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                await _transaction.CommitAsync(cancellationToken);
#else
                _transaction.Commit();
#endif
                return result;
            }
            catch (Exception)
            {
                if (!_transactionCompleted && _transaction?.Connection != null)
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    await _transaction.RollbackAsync(cancellationToken);
#else
                    _transaction.Rollback();
#endif
                    throw;
            }
            finally
            {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                if (_transaction != null)
                    await _transaction.DisposeAsync();

                if (_command?.Connection?.State == ConnectionState.Open)
                    await _command.Connection.DisposeAsync();

                if (_command != null)
                    await _command.DisposeAsync();
#else
                if (_transaction != null)
                    _transaction.Dispose();

                if (_command?.Connection?.State == ConnectionState.Open)
                    _command.Connection.Dispose();

                if (_command != null)
                    _command.Dispose();
#endif
                _transaction = null;
                _command = null;
            }
        }

        public async Task<bool> ExecuteTransaction(Func<IDatabase, Task<TransactionResult>> func)
        {
            await using var command = new DatabaseCommand(in Options);
            try
            {
                _transaction = await command.BeginTransactionAsync(CancellationToken.None);
                _command = command.CreateEmptyCommand();
                _command.Transaction = _transaction;
                var result = await func(this);
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                await _transaction.CommitAsync();
#else
                _transaction.Commit();
#endif
                return result == TransactionResult.Committed;
            }
            catch (Exception)
            {
                if (!_transactionCompleted && _transaction?.Connection != null)
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    await _transaction.RollbackAsync();
#else
                    _transaction.Rollback();
#endif
                throw;
            }
            finally
            {
                if (_transaction != null)
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    await _transaction.DisposeAsync();
#else
                    _transaction.Dispose();
#endif

                if (_command?.Connection?.State == ConnectionState.Open)
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    await _command.Connection.DisposeAsync();
#else
                    _command.Connection.Dispose();
#endif

                if (_command != null)
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    await _command.DisposeAsync();
#else
                    _command.Dispose();
#endif

                _transaction = null;
                _command = null;
            }
        }

        public async Task<bool> ExecuteTransaction(Func<IDatabase, CancellationToken, Task<TransactionResult>> func, CancellationToken cancellationToken)
        {
            await using var command = new DatabaseCommand(in Options);
            try
            {
                _transaction = await command.BeginTransactionAsync(cancellationToken);
                _command = command.CreateEmptyCommand();
                _command.Transaction = _transaction;
                var result = await func(this, cancellationToken);
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                await _transaction.CommitAsync(cancellationToken);
#else
                _transaction.Commit();
#endif
                return result == TransactionResult.Committed;
            }
            catch (Exception)
            {
                if (!_transactionCompleted && _transaction?.Connection != null)
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    await _transaction.RollbackAsync(cancellationToken);
#else
                    _transaction.Rollback();
#endif
                throw;
            }
            finally
            {
                if (_transaction != null)
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    await _transaction.DisposeAsync();
#else
                    _transaction.Dispose();
#endif

                if (_command?.Connection?.State == ConnectionState.Open)
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    await _command.Connection.DisposeAsync();
#else
                    _command.Connection.Dispose();
#endif

                if (_command != null)
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    await _command.DisposeAsync();
#else
                    _command.Dispose();
#endif

                _transaction = null;
                _command = null;
            }
        }

        public async Task<TransactionResult> CommitAsync()
        {
            if (_transaction == null)
                throw new InvalidOperationException("Transaction not started");

            _transactionCompleted = true;

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            await _transaction.CommitAsync();
            return TransactionResult.Committed;
#else
            _transaction.Commit();
            return await Task.FromResult(TransactionResult.Committed);
#endif
        }

        public async Task<TransactionResult> RollbackAsync()
        {
            if (_transaction == null)
                throw new InvalidOperationException("Transaction not started");

            _transactionCompleted = true;

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            await _transaction.RollbackAsync();
            return TransactionResult.Rollbacked;
#else
            _transaction.Rollback();
            return await Task.FromResult(TransactionResult.Rollbacked);
#endif
        }
        #endregion transaction

        #region without result data

        private static readonly DbCommandConfiguration QueryExecuteConfig = new DbCommandConfiguration(
                                                                               commandBehavior: CommandBehavior.Default,
                                                                               methodHandled: MethodHandled.Execute,
                                                                               keyAsReturnValue: false,
                                                                               skipAutoGeneratedColumn: false,
                                                                               generateParameterWithKeys: false);

        public int Execute(in string script, in object parameters = null)
        {
            using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryExecuteConfig, in _buffered, in _transaction, in _command);
            command.Prepare();
            var affected = command.ExecuteNonQuery();
            command.SetValuesOutFields();
            return affected;
        }

        public DbOpResult TryExecute(in string script, in object parameters = null)
        {
            DbOpResult response;

            try
            {
                response = Execute(in script, in parameters);
            }
            catch (Exception ex)
            {
                response = ErrorDetailMessage(in script, in ex, in parameters);
            }

            return response;
        }

        public async Task<DbOpAsyncResult> TryExecuteAsync(string script, object parameters, CancellationToken cancellationToken)
        {
            DbOpAsyncResult response;

            try
            {
                await using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryExecuteConfig, in _buffered, in _transaction, in _command);
                command.Prepare();
                response = await command.ExecuteNonQueryAsync(cancellationToken);
                command.SetValuesOutFields();
            }
            catch (Exception ex) when (ex is TaskCanceledException || DatabaseProvider.IsCancelatedOperationException(in ex))
            {
                response = DbOpAsyncResult.OperationCanceled<DbOpAsyncResult>();
            }
            catch (Exception ex)
            {
                response = ErrorDetailMessage(in script, in ex, in parameters);
            }

            return response;
        }

        public async Task<int> ExecuteAsync(string script, object parameters, CancellationToken cancellationToken)
        {
            await using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryExecuteConfig, in _buffered, in _transaction, in _command);
            command.Prepare();
            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            command.SetValuesOutFields();
            return affected;
        }

        public async Task<int> ExecuteAsync(string script, object parameters = null)
        {
            return await ExecuteAsync(script, parameters, CancellationToken.None);
        }

        public DbOpResult<T> TryExecuteScalar<T>(in string script, in object parameters = null)
        {
            DbOpResult<T> response;

            try
            {
                response = ExecuteScalar<T>(in script, in parameters);
            }
            catch (Exception ex)
            {
                response = ErrorDetailMessage(in script, in ex, in parameters);
            }

            return response;
        }

        public T ExecuteScalar<T>(in string script, in object parameters = null)
        {
            using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryExecuteConfig, in _buffered, in _transaction, in _command);
            command.Prepare();
            var rawValue = command.ExecuteScalar();
            command.SetValuesOutFields();
            return (T)TypeConversionRegistry.ConvertOutParameterValue(Options.SqlProvider, rawValue, typeof(T), true);
        }

        public async Task<T> ExecuteScalarAsync<T>(string script, object parameters = null)
        {
            return await ExecuteScalarAsync<T>(script, parameters, CancellationToken.None);
        }

        public async Task<T> ExecuteScalarAsync<T>(string script, object parameters, CancellationToken cancellationToken)
        {
            using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryExecuteConfig, in _buffered, in _transaction, in _command);
            command.Prepare();
            var rawValue = await command.ExecuteScalarAsync(cancellationToken);
            command.SetValuesOutFields();
            return (T)TypeConversionRegistry.ConvertOutParameterValue(Options.SqlProvider, rawValue, typeof(T), true);
        }

        public async Task<DbOpAsyncResult<T>> TryExecuteScalarAsync<T>(string script, object parameters = null)
        {
            return await TryExecuteScalarAsync<T>(script, parameters, CancellationToken.None);
        }

        public async Task<DbOpAsyncResult<T>> TryExecuteScalarAsync<T>(string script, object parameters, CancellationToken cancellationToken)
        {
            DbOpAsyncResult<T> response;

            try
            {
                response = await TryExecuteScalarAsync<T>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || DatabaseProvider.IsCancelatedOperationException(in ex))
            {
                response = DbOpAsyncResult.OperationCanceled<DbOpAsyncResult<T>>();
            }
            catch (Exception ex)
            {
                response = ErrorDetailMessage(in script, in ex, in parameters);
            }

            return response;
        }

        #endregion without result data

        #region single row result

        private static readonly DbCommandConfiguration QuerySingleConfig = new DbCommandConfiguration(
                                                                           commandBehavior: CommandBehavior.SingleRow | CommandBehavior.SequentialAccess,
                                                                           methodHandled: MethodHandled.FetchOneQueryString,
                                                                           keyAsReturnValue: false,
                                                                           skipAutoGeneratedColumn: false,
                                                                           generateParameterWithKeys: false);

        public T FetchOne<T>(in string script, in object parameters = null)
        {
            using var command = new DatabaseCommand(in Options, in script, in parameters, in QuerySingleConfig, in _buffered, in _transaction, in _command);
            command.Prepare();
            var item = command.ReadListItems<T>().FirstOrDefault();
            command.SetValuesOutFields();
            return item;
        }

        public T FetchOne<T>(Expression<Func<T, bool>> where = null)
        {
            var generator = new SQLGenerator<T>(in Formatter);
            var script = generator.GenerateSelectWhere(in where, SqlOperation.SelectSingle, out var filter);
            return FetchOne<T>(in script, in filter);
        }

        public DbOpResult<T> TryFetchOne<T>(in string script, in object parameters = null)
        {
            DbOpResult<T> response;

            try
            {
                response = FetchOne<T>(in script, in parameters);
            }
            catch (Exception ex)
            {
                response = ErrorDetailMessage(in script, in ex, in parameters);
            }

            return response;
        }

        public async Task<T> FetchOneAsync<T>(string script, object parameters, CancellationToken cancellationToken)
        {
            await using var command = new DatabaseCommand(in Options, in script, in parameters, in QuerySingleConfig, in _buffered, in _transaction, in _command);
            command.Prepare();
            return (await command.ReadListItemsAsync<T>(cancellationToken)).FirstOrDefault();
        }

        public async Task<T> FetchOneAsync<T>(string script, object parameters = null)
        {
            return await FetchOneAsync<T>(script, parameters, CancellationToken.None);
        }

        public async Task<DbOpAsyncResult<T>> TryFetchOneAsync<T>(string script, object parameters, CancellationToken cancellationToken)
        {
            DbOpAsyncResult<T> response;

            try
            {
                response = await FetchOneAsync<T>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || DatabaseProvider.IsCancelatedOperationException(in ex))
            {
                response = DbOpAsyncResult.OperationCanceled<DbOpAsyncResult<T>>();
            }
            catch (Exception ex)
            {
                response = ErrorDetailMessage(in script, in ex, in parameters);
            }

            return response;
        }

        #endregion single row result

        #region one result set

        private static readonly DbCommandConfiguration QueryListConfig = new DbCommandConfiguration(
                                                                        commandBehavior: CommandBehavior.SingleResult | CommandBehavior.SequentialAccess,
                                                                        methodHandled: MethodHandled.FetchListQueryString,
                                                                        keyAsReturnValue: false,
                                                                        skipAutoGeneratedColumn: false,
                                                                        generateParameterWithKeys: false);

        public List<T> FetchList<T>(in string script, in object parameters = null)
        {
            using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryListConfig, in _buffered, in _transaction, in _command);
            command.Prepare();
            var result = command.ReadListItems<T>();
            command.SetValuesOutFields();
            return result;
        }

        public List<T> FetchList<T>(Expression<Func<T, bool>> where = null)
        {
            var generator = new SQLGenerator<T>(in Formatter);
            string script = generator.GenerateSelectWhere(in where, SqlOperation.SelectList, out var filter);
            return FetchList<T>(in script, in filter);
        }

        public DbOpResult<List<T>> TryFetchList<T>(in string script, in object parameters = null)
        {
            DbOpResult<List<T>> response;

            try
            {
                response = FetchList<T>(in script, in parameters);
            }
            catch (Exception ex)
            {
                response = ErrorDetailMessage(in script, in ex, in parameters);
            }

            return response;
        }

        public async Task<List<T>> FetchListAsync<T>(string script, object parameters, CancellationToken cancellationToken)
        {
            await using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryListConfig, in _buffered, in _transaction, in _command);

            command.Prepare();

            var result = await command.ReadListItemsAsync<T>(cancellationToken);

            command.SetValuesOutFields();

            return result;
        }

        public async Task<List<T>> FetchListAsync<T>(string script, object parameters = null)
        {
            return await FetchListAsync<T>(script, parameters, CancellationToken.None);
        }

        public async Task<List<T>> FetchListAsync<T>(Expression<Func<T, bool>> where)
        {
            return await FetchListAsync<T>(where, CancellationToken.None);
        }

        public async Task<List<T>> FetchListAsync<T>(Expression<Func<T, bool>> where, CancellationToken cancellationToken)
        {
            var generator = new SQLGenerator<T>(in Formatter);
            var script = generator.GenerateSelectWhere(where, SqlOperation.SelectList, out var filter);
            return await FetchListAsync<T>(script, filter, cancellationToken);
        }

        public async Task<DbOpAsyncResult<List<T>>> TryFetchListAsync<T>(string script, object parameters, CancellationToken cancellationToken)
        {
            DbOpAsyncResult<List<T>> response;

            try
            {
                response = await FetchListAsync<T>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || DatabaseProvider.IsCancelatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCanceled<DbOpAsyncResult<List<T>>>();
            }
            catch (Exception ex)
            {
                response = ErrorDetailMessage(in script, in ex, in parameters);
            }

            return response;
        }

        #endregion one result set

        #region streaming

        public (Action, IEnumerable<List<T>>) FetchData<T>(string script, object parameters = null, int batchSize = 1000)
        {
            var command = new DatabaseCommand(in Options, in script, in parameters, in QueryListConfig, in _buffered, in _transaction, in _command);
            command.Prepare();
            return (command.Dispose, command.FetchData<T>(batchSize));
        }

        public (Action, IAsyncEnumerable<List<T>>) FetchDataAsync<T>(string script, object parameters, int batchSize = 1000)
        {
            var command = new DatabaseCommand(in Options, in script, in parameters, in QueryListConfig, in _buffered, in _transaction, in _command);
            command.Prepare();
            return (command.Dispose, command.FetchDataAsync<T>(batchSize, CancellationToken.None));
        }

        public (Action, IAsyncEnumerable<List<T>>) FetchDataAsync<T>(string script, object parameters, int batchSize, CancellationToken cancellationToken)
        {
            var command = new DatabaseCommand(in Options, in script, in parameters, in QueryListConfig, in _buffered, in _transaction, in _command);
            command.Prepare();
            return (command.Dispose, command.FetchDataAsync<T>(batchSize, cancellationToken));
        }

        #endregion streaming

        #region Multiple result set

        class CursorName
        {
            public string get_data { get; set; }
        }

        private static readonly DbCommandConfiguration QueryTuple2Config = new DbCommandConfiguration(
                                                                           commandBehavior: CommandBehavior.Default | CommandBehavior.SequentialAccess,
                                                                           methodHandled: MethodHandled.FetchTupleQueryString_2,
                                                                           keyAsReturnValue: false,
                                                                           skipAutoGeneratedColumn: false,
                                                                           generateParameterWithKeys: false);

        public DbOpResult<Tuple<List<T1>, List<T2>>> TryFetchTuple<T1, T2>(in string script, in object parameters = null)
        {
            DbOpResult<Tuple<List<T1>, List<T2>>> response;

            try
            {
                response = FetchTuple<T1, T2>(in script, in parameters);
            }
            catch (Exception ex)
            {
                response = ErrorDetailMessage(in script, in ex, in parameters);
            }

            return response;
        }

        public Tuple<List<T1>, List<T2>> FetchTuple<T1, T2>(in string script, in object parameters = null)
        {
            if (Options.SqlProvider == SqlProvider.PostgreSql)
            {
                var _script = script;
                var _parameters = parameters;
                return ExecuteTransaction((db) =>
                {
                    var cursorResult = db.FetchList<CursorName>(in _script, in _parameters);
                    var t1 = db.FetchList<T1>($"/*{_script}*/FETCH ALL FROM \"{cursorResult[0].get_data}\"");
                    var t2 = db.FetchList<T2>($"/*{_script}*/FETCH ALL FROM \"{cursorResult[1].get_data}\"");
                    return new Tuple<List<T1>, List<T2>>(t1, t2);
                });
            }
            else
            {
                using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryTuple2Config, in _buffered, in _transaction, in _command);
                command.Prepare();
                var t1 = command.ReadListItems<T1>();
                var t2 = command.ReadListNextItems<T2>();

                command.SetValuesOutFields();

                return new Tuple<List<T1>, List<T2>>(t1, t2);
            }
        }

        public async Task<Tuple<List<T1>, List<T2>>> FetchTupleAsync<T1, T2>(string script, object parameters, CancellationToken cancellationToken)
        {
            if (Options.SqlProvider == SqlProvider.PostgreSql)
            {
                var _script = script;
                var _parameters = parameters;
                return await ExecuteTransactionAsync(async (db, cancelToken) =>
                {
                    var cursorResult = await db.FetchListAsync<CursorName>(_script, _parameters, cancelToken);
                    var t1 = await db.FetchListAsync<T1>($"/*{_script}*/FETCH ALL FROM \"{cursorResult[0].get_data}\"", null, cancelToken);
                    var t2 = await db.FetchListAsync<T2>($"/*{_script}*/FETCH ALL FROM \"{cursorResult[1].get_data}\"", null, cancelToken);
                    return new Tuple<List<T1>, List<T2>>(t1, t2);
                }, cancellationToken);
            }
            else
            {
                await using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryTuple2Config, in _buffered, in _transaction, in _command);
                command.Prepare();

                var t1 = await command.ReadListItemsAsync<T1>(cancellationToken);
                var t2 = await command.ReadListNextItemsAsync<T2>(cancellationToken);

                command.SetValuesOutFields();

                return new Tuple<List<T1>, List<T2>>(t1, t2);
            }
           
        }

        public async Task<Tuple<List<T1>, List<T2>>> FetchTupleAsync<T1, T2>(string script, object parameters = null)
        {
            return await FetchTupleAsync<T1, T2>(script, parameters, CancellationToken.None);
        }

        public async Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>>>> TryFetchTupleAsync<T1, T2>(string script, object parameters, CancellationToken cancellationToken)
        {
            DbOpAsyncResult<Tuple<List<T1>, List<T2>>> response;

            try
            {
                response = await FetchTupleAsync<T1, T2>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || DatabaseProvider.IsCancelatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCanceled<DbOpAsyncResult<Tuple<List<T1>, List<T2>>>>();
            }
            catch (Exception ex)
            {
                response = ErrorDetailMessage(in script, in ex, in parameters);
            }

            return response;
        }

        private static readonly DbCommandConfiguration QueryTuple3Config = new DbCommandConfiguration(
                                                                           commandBehavior: CommandBehavior.Default | CommandBehavior.SequentialAccess,
                                                                           methodHandled: MethodHandled.FetchTupleQueryString_3,
                                                                           keyAsReturnValue: false,
                                                                           skipAutoGeneratedColumn: false,
                                                                           generateParameterWithKeys: false);

        public DbOpResult<Tuple<List<T1>, List<T2>, List<T3>>> TryFetchTuple<T1, T2, T3>(in string script, in object parameters = null)
        {
            DbOpResult<Tuple<List<T1>, List<T2>, List<T3>>> response;

            try
            {
                response = FetchTuple<T1, T2, T3>(in script, in parameters);
            }
            catch (Exception ex)
            {
                response = ErrorDetailMessage(in script, in ex, in parameters);
            }

            return response;
        }

        public Tuple<List<T1>, List<T2>, List<T3>> FetchTuple<T1, T2, T3>(in string script, in object parameters = null)
        {
            if (Options.SqlProvider == SqlProvider.PostgreSql)
            {
                var _script = script;
                var _parameters = parameters;
                return ExecuteTransaction((db) =>
                {
                    var cursorResult = db.FetchList<CursorName>(in _script, in _parameters);
                    var t1 = db.FetchList<T1>($"/*{_script}*/FETCH ALL FROM \"{cursorResult[0].get_data}\"");
                    var t2 = db.FetchList<T2>($"/*{_script}*/FETCH ALL FROM \"{cursorResult[1].get_data}\"");
                    var t3 = db.FetchList<T3>($"/*{_script}*/FETCH ALL FROM \"{cursorResult[2].get_data}\"");
                    return new Tuple<List<T1>, List<T2>, List<T3>>(t1, t2, t3);
                });
            }
            else
            {
                using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryTuple3Config, in _buffered, in _transaction, in _command);
                command.Prepare();

                var t1 = command.ReadListItems<T1>();
                var t2 = command.ReadListNextItems<T2>();
                var t3 = command.ReadListNextItems<T3>();

                command.SetValuesOutFields();

                return new Tuple<List<T1>, List<T2>, List<T3>>(t1, t2, t3);
            }
        }

        public async Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>>>> TryFetchTuple<T1, T2, T3>(string script, object parameters, CancellationToken cancellationToken)
        {
            DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>>> response;

            try
            {
                response = await FetchTupleAsync<T1, T2, T3>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || DatabaseProvider.IsCancelatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCanceled<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>>>>();
            }
            catch (Exception ex)
            {
                response = ErrorDetailMessage(in script, in ex, in parameters);
            }

            return response;
        }

        public async Task<Tuple<List<T1>, List<T2>, List<T3>>> FetchTupleAsync<T1, T2, T3>(string script, object parameters, CancellationToken cancellationToken)
        {
            if (Options.SqlProvider == SqlProvider.PostgreSql)
            {
                var _script = script;
                var _parameters = parameters;
                return await ExecuteTransactionAsync( async (db, cancelToken) =>
                {
                    var cursorResult = await db.FetchListAsync<CursorName>(_script, _parameters, cancelToken);
                    var t1 = await db.FetchListAsync<T1>($"/*{_script}*/FETCH ALL FROM \"{cursorResult[0].get_data}\"", null, cancelToken);
                    var t2 = await db.FetchListAsync<T2>($"/*{_script}*/FETCH ALL FROM \"{cursorResult[1].get_data}\"", null, cancelToken);
                    var t3 = await db.FetchListAsync<T3>($"/*{_script}*/FETCH ALL FROM \"{cursorResult[2].get_data}\"", null, cancelToken);
                    return new Tuple<List<T1>, List<T2>, List<T3>>(t1, t2, t3);
                }, cancellationToken);
            }
            else
            {
                await using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryTuple3Config, in _buffered, in _transaction, in _command);
                command.Prepare();

                var t1 = await command.ReadListItemsAsync<T1>(cancellationToken);
                var t2 = await command.ReadListNextItemsAsync<T2>(cancellationToken);
                var t3 = await command.ReadListNextItemsAsync<T3>(cancellationToken);

                command.SetValuesOutFields();

                return new Tuple<List<T1>, List<T2>, List<T3>>(t1, t2, t3);
            }
            
        }

        public async Task<Tuple<List<T1>, List<T2>, List<T3>>> FetchTupleAsync<T1, T2, T3>(string script, object parameters = null)
        {
            return await FetchTupleAsync<T1, T2, T3>(script, parameters, CancellationToken.None);
        }

        private static readonly DbCommandConfiguration QueryTuple4Config = new DbCommandConfiguration(
                                                                           commandBehavior: CommandBehavior.Default | CommandBehavior.SequentialAccess,
                                                                           methodHandled: MethodHandled.FetchTupleQueryString_4,
                                                                           keyAsReturnValue: false,
                                                                           skipAutoGeneratedColumn: false,
                                                                           generateParameterWithKeys: false);

        public DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> TryFetchTuple<T1, T2, T3, T4>(in string script, in object parameters)
        {
            DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> response;

            try
            {
                response = FetchTuple<T1, T2, T3, T4>(in script, in parameters);
            }
            catch (Exception ex)
            {
                response = ErrorDetailMessage(in script, in ex, in parameters);
            }

            return response;
        }

        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>> FetchTuple<T1, T2, T3, T4>(in string script, in object parameters = null)
        {
            if (Options.SqlProvider == SqlProvider.PostgreSql)
            {
                var _script = script;
                var _parameters = parameters;
                return ExecuteTransaction((db) =>
                {
                    var cursorResult = db.FetchList<CursorName>(in _script, in _parameters);
                    var t1 = db.FetchList<T1>($"/*{_script}*/FETCH ALL FROM \"{cursorResult[0].get_data}\"");
                    var t2 = db.FetchList<T2>($"/*{_script}*/FETCH ALL FROM \"{cursorResult[1].get_data}\"");
                    var t3 = db.FetchList<T3>($"/*{_script}*/FETCH ALL FROM \"{cursorResult[2].get_data}\"");
                    var t4 = db.FetchList<T4>($"/*{_script}*/FETCH ALL FROM \"{cursorResult[3].get_data}\"");
                    return new Tuple<List<T1>, List<T2>, List<T3>, List<T4>>(t1, t2, t3, t4);
                });
            }
            else
            {
                using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryTuple4Config, in _buffered, in _transaction, in _command);
                command.Prepare();

                var t1 = command.ReadListItems<T1>();
                var t2 = command.ReadListNextItems<T2>();
                var t3 = command.ReadListNextItems<T3>();
                var t4 = command.ReadListNextItems<T4>();

                command.SetValuesOutFields();

                return new Tuple<List<T1>, List<T2>, List<T3>, List<T4>>(t1, t2, t3, t4);
            }
            
        }

        public async Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>>> TryFetchTuple<T1, T2, T3, T4>(string script, object parameters, CancellationToken cancellationToken)
        {
            DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> response;

            try
            {
                response = await FetchTupleAsync<T1, T2, T3, T4>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || DatabaseProvider.IsCancelatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCanceled<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>>>();
            }
            catch (Exception ex)
            {
                response = ErrorDetailMessage(in script, in ex, in parameters);
            }

            return response;
        }

        public async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> FetchTupleAsync<T1, T2, T3, T4>(string script, object parameters, CancellationToken cancellationToken)
        {
            if (Options.SqlProvider == SqlProvider.PostgreSql)
            {
                var _script = script;
                var _parameters = parameters;
                return await ExecuteTransactionAsync( async (db, cancelToken) =>
                {
                    var cursorResult = await db.FetchListAsync<CursorName>(_script, _parameters, cancelToken);
                    var t1 = await db.FetchListAsync<T1>($"/*{_script}*/FETCH ALL FROM \"{cursorResult[0].get_data}\"", null, cancelToken);
                    var t2 = await db.FetchListAsync<T2>($"/*{_script}*/FETCH ALL FROM \"{cursorResult[1].get_data}\"", null, cancelToken);
                    var t3 = await db.FetchListAsync<T3>($"/*{_script}*/FETCH ALL FROM \"{cursorResult[2].get_data}\"", null, cancelToken);
                    var t4 = await db.FetchListAsync<T4>($"/*{_script}*/FETCH ALL FROM \"{cursorResult[3].get_data}\"", null, cancelToken);
                    return new Tuple<List<T1>, List<T2>, List<T3>, List<T4>>(t1, t2, t3, t4);
                }, cancellationToken);
            }
            else
            {
                await using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryTuple4Config, in _buffered, in _transaction, in _command);
                command.Prepare();

                var t1 = await command.ReadListItemsAsync<T1>(cancellationToken);
                var t2 = await command.ReadListNextItemsAsync<T2>(cancellationToken);
                var t3 = await command.ReadListNextItemsAsync<T3>(cancellationToken);
                var t4 = await command.ReadListNextItemsAsync<T4>(cancellationToken);

                command.SetValuesOutFields();

                return new Tuple<List<T1>, List<T2>, List<T3>, List<T4>>(t1, t2, t3, t4);
            }
        }

        public async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> FetchTupleAsync<T1, T2, T3, T4>(string script, object parameters = null)
        {
            return await FetchTupleAsync<T1, T2, T3, T4>(script, parameters, CancellationToken.None);
        }

        private static readonly DbCommandConfiguration QueryTuple5Config = new DbCommandConfiguration(
                                                                           commandBehavior: CommandBehavior.Default | CommandBehavior.SequentialAccess,
                                                                           methodHandled: MethodHandled.FetchTupleQueryString_5,
                                                                           keyAsReturnValue: false,
                                                                           skipAutoGeneratedColumn: false,
                                                                           generateParameterWithKeys: false);

        public DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>> TryFetchTuple<T1, T2, T3, T4, T5>(in string script, in object parameters = null)
        {
            DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>> response;

            try
            {
                response = FetchTuple<T1, T2, T3, T4, T5>(in script, in parameters);
            }
            catch (Exception ex)
            {
                response = ErrorDetailMessage(in script, in ex, in parameters);
            }

            return response;
        }

        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>> FetchTuple<T1, T2, T3, T4, T5>(in string script, in object parameters = null)
        {
            if (Options.SqlProvider == SqlProvider.PostgreSql)
            {
                var _script = script;
                var _parameters = parameters;
                return ExecuteTransaction((db) =>
                {
                    var cursorResult = db.FetchList<CursorName>(in _script, in _parameters);
                    var t1 = db.FetchList<T1>($"/*{_script}*/FETCH ALL FROM \"{cursorResult[0].get_data}\"");
                    var t2 = db.FetchList<T2>($"/*{_script}*/FETCH ALL FROM \"{cursorResult[1].get_data}\"");
                    var t3 = db.FetchList<T3>($"/*{_script}*/FETCH ALL FROM \"{cursorResult[2].get_data}\"");
                    var t4 = db.FetchList<T4>($"/*{_script}*/FETCH ALL FROM \"{cursorResult[3].get_data}\"");
                    var t5 = db.FetchList<T5>($"/*{_script}*/FETCH ALL FROM \"{cursorResult[4].get_data}\"");
                    return new Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>(t1, t2, t3, t4, t5);
                });
            }
            else
            {
                using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryTuple5Config, in _buffered, in _transaction, in _command);
                command.Prepare();

                var t1 = command.ReadListItems<T1>();
                var t2 = command.ReadListNextItems<T2>();
                var t3 = command.ReadListNextItems<T3>();
                var t4 = command.ReadListNextItems<T4>();
                var t5 = command.ReadListNextItems<T5>();

                command.SetValuesOutFields();

                return new Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>(t1, t2, t3, t4, t5);
            }
        }

        public async Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>>> TryFetchTuple<T1, T2, T3, T4, T5>(string script, object parameters, CancellationToken cancellationToken)
        {
            DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>> response;

            try
            {
                response = await FetchTupleAsync<T1, T2, T3, T4, T5>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || DatabaseProvider.IsCancelatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCanceled<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>>>();
            }
            catch (Exception ex)
            {
                response = ErrorDetailMessage(in script, in ex, in parameters);
            }

            return response;
        }

        public async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>> FetchTupleAsync<T1, T2, T3, T4, T5>(string script, object parameters, CancellationToken cancellationToken)
        {
            await using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryTuple5Config, in _buffered, in _transaction, in _command);
            command.Prepare();

            var t1 = await command.ReadListItemsAsync<T1>(cancellationToken);
            var t2 = await command.ReadListNextItemsAsync<T2>(cancellationToken);
            var t3 = await command.ReadListNextItemsAsync<T3>(cancellationToken);
            var t4 = await command.ReadListNextItemsAsync<T4>(cancellationToken);
            var t5 = await command.ReadListNextItemsAsync<T5>(cancellationToken);

            command.SetValuesOutFields();

            return new Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>(t1, t2, t3, t4, t5);
        }

        public async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>> FetchTupleAsync<T1, T2, T3, T4, T5>(string script, object parameters = null)
        {
            return await FetchTupleAsync<T1, T2, T3, T4, T5>(script, parameters, CancellationToken.None);
        }

        private static readonly DbCommandConfiguration QueryTuple6Config = new DbCommandConfiguration(
                                                                           commandBehavior: CommandBehavior.Default | CommandBehavior.SequentialAccess,
                                                                           methodHandled: MethodHandled.FetchTupleQueryString_6,
                                                                           keyAsReturnValue: false,
                                                                           skipAutoGeneratedColumn: false,
                                                                           generateParameterWithKeys: false);

        public DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>> TryFetchTuple<T1, T2, T3, T4, T5, T6>(in string script, in object parameters = null)
        {
            DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>> response;

            try
            {
                response = FetchTuple<T1, T2, T3, T4, T5, T6>(in script, in parameters);
            }
            catch (Exception ex)
            {
                response = ErrorDetailMessage(in script, in ex, in parameters);
            }

            return response;
        }

        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>> FetchTuple<T1, T2, T3, T4, T5, T6>(in string script, in object parameters = null)
        {
            using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryTuple6Config, in _buffered, in _transaction, in _command);
            command.Prepare();

            var t1 = command.ReadListItems<T1>();
            var t2 = command.ReadListNextItems<T2>();
            var t3 = command.ReadListNextItems<T3>();
            var t4 = command.ReadListNextItems<T4>();
            var t5 = command.ReadListNextItems<T5>();
            var t6 = command.ReadListNextItems<T6>();

            command.SetValuesOutFields();

            return new Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>(t1, t2, t3, t4, t5, t6);
        }

        public async Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>>> TryFetchTuple<T1, T2, T3, T4, T5, T6>(string script, object parameters, CancellationToken cancellationToken)
        {
            DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>> response;

            try
            {
                response = await FetchTupleAsync<T1, T2, T3, T4, T5, T6>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || DatabaseProvider.IsCancelatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCanceled<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>>>();
            }
            catch (Exception ex)
            {
                response = ErrorDetailMessage(in script, in ex, in parameters);
            }

            return response;
        }

        public async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>> FetchTupleAsync<T1, T2, T3, T4, T5, T6>(string script, object parameters, CancellationToken cancellationToken)
        {
            await using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryTuple6Config, in _buffered, in _transaction, in _command);
            command.Prepare();

            var t1 = await command.ReadListItemsAsync<T1>(cancellationToken);
            var t2 = await command.ReadListNextItemsAsync<T2>(cancellationToken);
            var t3 = await command.ReadListNextItemsAsync<T3>(cancellationToken);
            var t4 = await command.ReadListNextItemsAsync<T4>(cancellationToken);
            var t5 = await command.ReadListNextItemsAsync<T5>(cancellationToken);
            var t6 = await command.ReadListNextItemsAsync<T6>(cancellationToken);

            command.SetValuesOutFields();

            return new Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>(t1, t2, t3, t4, t5, t6);
        }

        public async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>> FetchTupleAsync<T1, T2, T3, T4, T5, T6>(string script, object parameters = null)
        {
            return await FetchTupleAsync<T1, T2, T3, T4, T5, T6>(script, parameters, CancellationToken.None);
        }

        private static readonly DbCommandConfiguration QueryTuple7Config = new DbCommandConfiguration(
                                                                           commandBehavior: CommandBehavior.Default | CommandBehavior.SequentialAccess,
                                                                           methodHandled: MethodHandled.FetchTupleQueryString_7,
                                                                           keyAsReturnValue: false,
                                                                           skipAutoGeneratedColumn: false,
                                                                           generateParameterWithKeys: false);

        public DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> TryFetchTuple<T1, T2, T3, T4, T5, T6, T7>(in string script, in object parameters = null)
        {
            DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> response;

            try
            {
                response = FetchTuple<T1, T2, T3, T4, T5, T6, T7>(in script, in parameters);
            }
            catch (Exception ex)
            {
                response = ErrorDetailMessage(in script, in ex, in parameters);
            }

            return response;
        }

        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>> FetchTuple<T1, T2, T3, T4, T5, T6, T7>(in string script, in object parameters = null)
        {
            using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryTuple7Config, in _buffered, in _transaction, in _command);
            command.Prepare();

            var t1 = command.ReadListItems<T1>();
            var t2 = command.ReadListNextItems<T2>();
            var t3 = command.ReadListNextItems<T3>();
            var t4 = command.ReadListNextItems<T4>();
            var t5 = command.ReadListNextItems<T5>();
            var t6 = command.ReadListNextItems<T6>();
            var t7 = command.ReadListNextItems<T7>();

            command.SetValuesOutFields();

            return new Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>(t1, t2, t3, t4, t5, t6, t7);
        }

        public async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> FetchTupleAsync<T1, T2, T3, T4, T5, T6, T7>(string script, object parameters, CancellationToken cancellationToken)
        {
            await using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryTuple7Config, in _buffered, in _transaction, in _command);
            command.Prepare();

            var t1 = await command.ReadListItemsAsync<T1>(cancellationToken);
            var t2 = await command.ReadListNextItemsAsync<T2>(cancellationToken);
            var t3 = await command.ReadListNextItemsAsync<T3>(cancellationToken);
            var t4 = await command.ReadListNextItemsAsync<T4>(cancellationToken);
            var t5 = await command.ReadListNextItemsAsync<T5>(cancellationToken);
            var t6 = await command.ReadListNextItemsAsync<T6>(cancellationToken);
            var t7 = await command.ReadListNextItemsAsync<T7>(cancellationToken);

            command.SetValuesOutFields();

            return new Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>(t1, t2, t3, t4, t5, t6, t7);
        }

        public async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> FetchTupleAsync<T1, T2, T3, T4, T5, T6, T7>(string script, object parameters = null)
        {
            return await FetchTupleAsync<T1, T2, T3, T4, T5, T6, T7>(script, parameters, CancellationToken.None);
        }

        public async Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>>> TryFetchTuple<T1, T2, T3, T4, T5, T6, T7>(string script, object parameters, CancellationToken cancellationToken)
        {
            var response = new DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>>();

            try
            {
                response.Result = await FetchTupleAsync<T1, T2, T3, T4, T5, T6, T7>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || DatabaseProvider.IsCancelatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCanceled<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>>>();
            }
            catch (Exception ex)
            {
                response = ErrorDetailMessage(in script, in ex, in parameters);
            }

            return response;
        }

        #endregion Multiple result set

        #region Error Handling

        private string ErrorDetailMessage(in string script, in Exception ex, in object value = null)
        {
            if (!Options.DetailErrorMessage)
            {
                return ex.Message;
            }

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Store Procedure/Script:")
                         .AppendLine("\t" + script);

            if (value != null && !Options.HideSensibleDataValue)
            {
                stringBuilder.AppendLine("Parameters:");

                foreach (var parameter in value.GetType().GetProperties())
                {
                    stringBuilder.AppendLine(ErrorFormat(in value, parameter));
                }
            }

            stringBuilder.AppendLine("Exception Message:")
                         .AppendLine("\t" + ex.Message);

            if (ex.InnerException != null)
            {
                stringBuilder.AppendLine("Inner Exception Message :")
                             .AppendLine("\t" + ex.InnerException);
            }

            stringBuilder.AppendLine();
            return stringBuilder.ToString();

            static string ErrorFormat(in object val, PropertyInfo info)
            {
                return "\t" + info.Name + " : " + (info.GetValue(val) ?? "NULL") + " ";
            }
        }

        #endregion Error Handling

        #region Write operations

        private static readonly DbCommandConfiguration AddConfig = new DbCommandConfiguration(
                                                                   commandBehavior: CommandBehavior.Default,
                                                                   methodHandled: MethodHandled.Execute,
                                                                   keyAsReturnValue: false,
                                                                   skipAutoGeneratedColumn: true,
                                                                   generateParameterWithKeys: false);

        public void Insert<T>(T entity)
        {
            var generator = new SQLGenerator<T>(in Formatter);
            var script = generator.GenerateInsert(generateKeyValue: false);
            using var command = new DatabaseCommand(in Options, in script, entity, in AddConfig, in _buffered, in _transaction, in _command);
            command.Prepare();
            command.ExecuteNonQuery();
        }

        private static readonly DbCommandConfiguration AddReturnIDConfig = new DbCommandConfiguration(
                                                                           commandBehavior: CommandBehavior.Default,
                                                                           methodHandled: MethodHandled.Execute,
                                                                           keyAsReturnValue: true,
                                                                           skipAutoGeneratedColumn: true,
                                                                           generateParameterWithKeys: false);

        public TE Insert<T, TE>(T entity)
        {
            var generator = new SQLGenerator<T>(in Formatter);

            if (Options.SqlProvider == SqlProvider.MySql && Options.PrepareStatements)
            {
                var scriptForMysql = generator.GenerateInsert(generateKeyValue: false);
                return ExecuteTransaction((db) =>
                {
                    db.Execute(scriptForMysql, entity);
                    return db.ExecuteScalar<TE>("SELECT LAST_INSERT_ID()");
                });
            }

            var script = generator.GenerateInsert(generateKeyValue: true);

            if (Options.SqlProvider == SqlProvider.Sqlite)
                return ExecuteTransaction((db) => db.ExecuteScalar<TE>(script, entity));

            using var command = new DatabaseCommand(in Options, in script, entity, in AddReturnIDConfig, in _buffered, in _transaction, in _command);
            command.Prepare();

            object rawValue = null;
            if (Options.SqlProvider == SqlProvider.Oracle)
            {
                command.ExecuteNonQuery();
                var param = command.OutParameters.First();
                rawValue = DatabaseProvider.GetValueFromOracleParameter(param, typeof(TE));
            }
            else
            {
                rawValue = command.ExecuteScalar();
            }

            return (TE)TypeConversionRegistry.ConvertOutParameterValue(Options.SqlProvider, rawValue, typeof(TE), true);
        }

        private static readonly DbCommandConfiguration UpdateConfig = new DbCommandConfiguration(
                                                                      commandBehavior: CommandBehavior.Default,
                                                                      methodHandled: MethodHandled.Execute,
                                                                      keyAsReturnValue: false,
                                                                      skipAutoGeneratedColumn: false,
                                                                      generateParameterWithKeys: false);

        public void Update<T>(T entity)
        {
            var generator = new SQLGenerator<T>(in Formatter);
            var script = generator.GenerateUpdate();
            using var command = new DatabaseCommand(in Options, in script, entity, in UpdateConfig, in _buffered, in _transaction, in _command);
            command.Prepare();
            command.ExecuteNonQuery();
        }

        private static readonly DbCommandConfiguration DeleteConfig = new DbCommandConfiguration(
                                                                         commandBehavior: CommandBehavior.Default,
                                                                         methodHandled: MethodHandled.Execute,
                                                                         keyAsReturnValue: false,
                                                                         skipAutoGeneratedColumn: false,
                                                                         generateParameterWithKeys: true);

        public void Delete<T>(T entity)
        {
            var generator = new SQLGenerator<T>(in Formatter);
            var script = generator.GenerateDelete();
            using var command = new DatabaseCommand(in Options, in script, entity, in DeleteConfig, in _buffered, in _transaction, in _command);
            command.Prepare();
            command.ExecuteNonQuery();
        }

        #endregion Write operations

        public static void Clear()
        {
            DynamicQueryInfo.Clear();
            DatabaseHelperProvider.CommandMetadata.Clear();

            foreach (var type in CacheTypeHash.CachedTypes)
            {
                if (type != null)
                {
                    var genericType = typeof(CacheTypeParser<>).MakeGenericType(type);
                    var method = genericType.GetMethod("Clear", BindingFlags.NonPublic | BindingFlags.Static);
                    method.Invoke(null, null);
                }
            }

            CacheTypeHash.CachedTypes.Clear();
            CacheTypeHash.CachedTypes = new HashSet<Type>();
        }
    }

    public enum TransactionResult : byte
    {
        Committed = 0,
        Rollbacked = 1
    }
}