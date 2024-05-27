﻿using System;
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
using Thomas.Database.Database;
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
            _command = command.CreateEmptyCommand();
            await func(this);
        }

        public async Task ExecuteBlockAsync(Func<IDatabase, CancellationToken, Task> func, CancellationToken cancellationToken)
        {
            await using var command = new DatabaseCommand(in Options);
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
            }
        }

        public bool ExecuteTransaction(Func<IDatabase, TransactionResult> func)
        {
            using var command = new DatabaseCommand(in Options);
            _transaction = command.BeginTransaction();
            _command = command.CreateEmptyCommand();

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

        public async Task<T> ExecuteTransactionAsync<T>(Func<IDatabase, CancellationToken, Task<T>> func, CancellationToken cancellationToken)
        {
            await using var command = new DatabaseCommand(in Options);
            try
            {
                _transaction = await command.BeginTransactionAsync(cancellationToken);
                _command = command.CreateEmptyCommand();
                var result = await func(this, cancellationToken);
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                await _transaction.CommitAsync(cancellationToken);
#else
                _transaction.Commit();
#endif
                return result;
            }
            catch (Exception ex)
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
            }
        }

        public async Task<bool> ExecuteTransaction(Func<IDatabase, CancellationToken, Task<TransactionResult>> func, CancellationToken cancellationToken)
        {
            await using var command = new DatabaseCommand(in Options);
            try
            {
                _transaction = await command.BeginTransactionAsync(cancellationToken);
                _command = command.CreateEmptyCommand();
                var result = await func(this, cancellationToken);
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                await _transaction.CommitAsync(cancellationToken);
#else
                _transaction.Commit();
#endif
                return result == TransactionResult.Committed;
            }
            catch (Exception ex)
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
            }
        }

        public async Task<TransactionResult> CommitAsync()
        {
            if (_transaction == null)
                throw new InvalidOperationException("Transaction not started");

            _transactionCompleted = true;

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            await _transaction.CommitAsync();
#else
            _transaction.Commit();
#endif
            return TransactionResult.Committed;
        }

        public async Task<TransactionResult> RollbackAsync()
        {
            if (_transaction == null)
                throw new InvalidOperationException("Transaction not started");

            _transactionCompleted = true;

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            await _transaction.RollbackAsync();
#else
            _transaction.Rollback();
#endif
            return TransactionResult.Rollbacked;
        }
#endregion transaction

        #region without result data

        private static readonly DbCommandConfiguration QueryExecuteConfig = new DbCommandConfiguration(
                                                                               commandBehavior: CommandBehavior.Default,
                                                                               methodHandled: MethodHandled.Execute,
                                                                               keyAsReturnValue: false,
                                                                               generateParameterWithKeys: false);

        public int Execute(in string script, in object? parameters = null)
        {
            using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryExecuteConfig, in _buffered, in _transaction, in _command);
            command.Prepare();
            var affected = command.ExecuteNonQuery();
            command.SetValuesOutFields();
            return affected;
        }

        public DbOpResult ExecuteOp(in string script, in object? parameters = null)
        {
            var response = new DbOpResult() { Success = true };

            try
            {
                response.RowsAffected = Execute(in script, in parameters);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(in script, in ex, in parameters);
                response = DbOpResult.ErrorResult<DbOpResult>(in msg);
            }

            return response;
        }

        public async Task<DbOpAsyncResult> ExecuteOpAsync(string script, object? parameters, CancellationToken cancellationToken)
        {
            var response = new DbOpAsyncResult() { Success = true };

            try
            {
                await using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryExecuteConfig, in _buffered, in _transaction, in _command);
                await command.PrepareAsync(cancellationToken);
                response.RowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
                command.SetValuesOutFields();
            }
            catch (Exception ex) when (ex is TaskCanceledException || DatabaseProvider.IsCancellatedOperationException(in ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(in script, in ex, in parameters);
                response = DbOpResult.ErrorResult<DbOpAsyncResult>(in msg);
            }

            return response;
        }

        public async Task<int> ExecuteAsync(string script, object? parameters, CancellationToken cancellationToken)
        {
            await using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryExecuteConfig, in _buffered, in _transaction, in _command);
            await command.PrepareAsync(cancellationToken);
            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            command.SetValuesOutFields();
            return affected;
        }

        public async Task<int> ExecuteAsync(string script, object? parameters = null)
        {
            return await ExecuteAsync(script, parameters, CancellationToken.None);
        }

        #endregion without result data

        #region single row result

        private static readonly DbCommandConfiguration QuerySingleConfig = new DbCommandConfiguration(
                                                                           commandBehavior: CommandBehavior.SingleRow,
                                                                           methodHandled: MethodHandled.ToListQueryString,
                                                                           keyAsReturnValue: false,
                                                                           generateParameterWithKeys: false);

        public T ToSingle<T>(in string script, in object? parameters = null)
        {
            using var command = new DatabaseCommand(in Options, in script, in parameters, in QuerySingleConfig, in _buffered, in _transaction, in _command);
            command.Prepare();
            var item = command.ReadListItems<T>().FirstOrDefault();
            command.SetValuesOutFields();
            return item;
        }

        public T ToSingle<T>(Expression<Func<T, bool>>? where = null)
        {
            var generator = new SqlGenerator<T>(in Formatter);
            object filter = null;
            var script = generator.GenerateSelectWhere(in where, SqlOperation.SelectSingle, ref filter);
            return ToSingle<T>(in script, in filter);
        }

        public DbOpResult<T> ToSingleOp<T>(in string script, in object? parameters = null)
        {
            var response = new DbOpResult<T>() { Success = true };

            try
            {
                response.Result = ToSingle<T>(in script, in parameters);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(in script, in ex, in parameters);
                response = DbOpResult.ErrorResult<DbOpResult<T>>(in msg);
            }

            return response;
        }


        public async Task<T> ToSingleAsync<T>(string script, object? parameters, CancellationToken cancellationToken)
        {
            await using var command = new DatabaseCommand(in Options, in script, in parameters, in QuerySingleConfig, in _buffered, in _transaction, in _command);
            await command.PrepareAsync(cancellationToken);
            return command.ReadListItems<T>().FirstOrDefault();
        }

        public async Task<T> ToSingleAsync<T>(string script, object? parameters = null)
        {
            return await ToSingleAsync<T>(script, parameters, CancellationToken.None);
        }

        public async Task<DbOpAsyncResult<T>> ToSingleOpAsync<T>(string script, object? parameters, CancellationToken cancellationToken)
        {
            var response = new DbOpAsyncResult<T>() { Success = true };

            try
            {
                response.Result = await ToSingleAsync<T>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || DatabaseProvider.IsCancellatedOperationException(in ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult<T>>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(in script, in ex, in parameters);
                response = DbOpResult.ErrorResult<DbOpAsyncResult<T>>(in msg);
            }

            return response;
        }

        #endregion single row result

        #region one result set

        private static readonly DbCommandConfiguration QueryListConfig = new DbCommandConfiguration(
                                                                        commandBehavior: CommandBehavior.SingleResult,
                                                                        methodHandled: MethodHandled.ToListQueryString,
                                                                        keyAsReturnValue: false,
                                                                        generateParameterWithKeys: false);

        public List<T> ToList<T>(in string script, in object? parameters = null)
        {
            using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryListConfig, in _buffered, in _transaction, in _command);
            command.Prepare();
            var result = command.ReadListItems<T>();
            command.SetValuesOutFields();
            return result;
        }

        public List<T> ToList<T>(Expression<Func<T, bool>>? where = null)
        {
            var generator = new SqlGenerator<T>(in Formatter);
            object? filter = null;
            string script = generator.GenerateSelectWhere(in where, SqlOperation.SelectList, ref filter);
            return ToList<T>(in script, in filter);
        }

        public DbOpResult<List<T>> ToListOp<T>(in string script, in object? parameters = null)
        {
            var response = new DbOpResult<List<T>>() { Success = true };

            try
            {
                response.Result = ToList<T>(in script, in parameters);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(in script, in ex, in parameters);
                response = DbOpResult.ErrorResult<DbOpResult<List<T>>>(in msg);
            }

            return response;
        }


        public async Task<List<T>> ToListAsync<T>(string script, object? parameters, CancellationToken cancellationToken)
        {
            await using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryListConfig, in _buffered, in _transaction, in _command);

            await command.PrepareAsync(cancellationToken);

            var result = await command.ReadListItemsAsync<T>(cancellationToken);

            command.SetValuesOutFields();

            return result;
        }

        public async Task<List<T>> ToListAsync<T>(string script, object? parameters = null)
        {
            return await ToListAsync<T>(script, parameters, CancellationToken.None);
        }

        public async Task<List<T>> ToListAsync<T>(Expression<Func<T, bool>>? where)
        {
            return await ToListAsync<T>(where, CancellationToken.None);
        }

        public async Task<List<T>> ToListAsync<T>(Expression<Func<T, bool>> where, CancellationToken cancellationToken)
        {
            var generator = new SqlGenerator<T>(in Formatter);
            object filter = null;
            var script = generator.GenerateSelectWhere(where, SqlOperation.SelectList, ref filter);
            return await ToListAsync<T>(script, filter, cancellationToken);
        }

        public async Task<DbOpAsyncResult<List<T>>> ToListOpAsync<T>(string script, object? parameters, CancellationToken cancellationToken)
        {
            var response = new DbOpAsyncResult<List<T>>() { Success = true };

            try
            {
                response.Result = await ToListAsync<T>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || DatabaseProvider.IsCancellatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult<List<T>>>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(in script, in ex, in parameters);
                response = DbOpResult.ErrorResult<DbOpAsyncResult<List<T>>>(msg);
            }

            return response;
        }

        #endregion one result set

        #region streaming

        /// <summary>
        /// stream data from database to avoid allocate to much memory in case of large data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="script"></param>
        /// <param name="parameters"></param>
        /// <param name="batchSize"></param>
        /// <returns>
        /// The action to dispose explicitly connection
        /// Enumerable of list of T streamed from database
        /// </returns>
        public (Action, IEnumerable<List<T>>) FetchData<T>(string script, object? parameters = null, int batchSize = 1000)
        {
            var command = new DatabaseCommand(in Options, in script, in parameters, in QueryListConfig, in _buffered, in _transaction, in _command);
            command.Prepare();
            return (command.Dispose, command.FetchData<T>(batchSize));
        }

        #endregion streaming

        #region Multiple result set

        private static readonly DbCommandConfiguration QueryTuple2Config = new DbCommandConfiguration(
                                                                           commandBehavior: CommandBehavior.Default,
                                                                           methodHandled: MethodHandled.ToTupleQueryString_2,
                                                                           keyAsReturnValue: false,
                                                                           generateParameterWithKeys: false);

        public DbOpResult<Tuple<List<T1>, List<T2>>> ToTupleOp<T1, T2>(in string script, in object? parameters = null)
        {
            var response = new DbOpResult<Tuple<List<T1>, List<T2>>>();

            try
            {
                response.Result = ToTuple<T1, T2>(in script, in parameters);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(in script, in ex, in parameters);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<List<T1>, List<T2>>>>(in msg);
            }

            return response;
        }

        public Tuple<List<T1>, List<T2>> ToTuple<T1, T2>(in string script, in object? parameters = null)
        {
            using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryTuple2Config, in _buffered, in _transaction, in _command);
            command.Prepare();
            var t1 = command.ReadListItems<T1>();
            var t2 = command.ReadListNextItems<T2>();

            command.SetValuesOutFields();

            return new Tuple<List<T1>, List<T2>>(t1, t2);
        }

        public async Task<Tuple<List<T1>, List<T2>>> ToTupleAsync<T1, T2>(string script, object? parameters, CancellationToken cancellationToken)
        {
            await using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryTuple2Config, in _buffered, in _transaction, in _command);
            await command.PrepareAsync(cancellationToken);

            var t1 = await command.ReadListItemsAsync<T1>(cancellationToken);
            var t2 = await command.ReadListNextItemsAsync<T2>(cancellationToken);

            command.SetValuesOutFields();

            return new Tuple<List<T1>, List<T2>>(t1, t2);
        }

        public async Task<Tuple<List<T1>, List<T2>>> ToTupleAsync<T1, T2>(string script, object? parameters = null)
        {
            return await ToTupleAsync<T1, T2>(script, parameters, CancellationToken.None);
        }

        public async Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>>>> ToTupleOpAsync<T1, T2>(string script, object? parameters, CancellationToken cancellationToken)
        {
            var response = new DbOpAsyncResult<Tuple<List<T1>, List<T2>>>();

            try
            {
                response.Result = await ToTupleAsync<T1, T2>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || DatabaseProvider.IsCancellatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult<Tuple<List<T1>, List<T2>>>>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(in script, in ex, in parameters);
                response = DbOpResult.ErrorResult<DbOpAsyncResult<Tuple<List<T1>, List<T2>>>>(msg);
            }

            return response;
        }

        private static readonly DbCommandConfiguration QueryTuple3Config = new DbCommandConfiguration(
                                                                           commandBehavior: CommandBehavior.Default,
                                                                           methodHandled: MethodHandled.ToTupleQueryString_3,
                                                                           keyAsReturnValue: false,
                                                                           generateParameterWithKeys: false);

        public DbOpResult<Tuple<List<T1>, List<T2>, List<T3>>> ToTupleOp<T1, T2, T3>(in string script, in object? parameters = null)
        {
            var response = new DbOpResult<Tuple<List<T1>, List<T2>, List<T3>>>();

            try
            {
                response.Result = ToTuple<T1, T2, T3>(in script, in parameters);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(in script, in ex, in parameters);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<List<T1>, List<T2>, List<T3>>>>(in msg);
            }

            return response;
        }

        public Tuple<List<T1>, List<T2>, List<T3>> ToTuple<T1, T2, T3>(in string script, in object? parameters = null)
        {
            using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryTuple3Config, in _buffered, in _transaction, in _command);
            command.Prepare();

            var t1 = command.ReadListItems<T1>();
            var t2 = command.ReadListNextItems<T2>();
            var t3 = command.ReadListNextItems<T3>();

            command.SetValuesOutFields();

            return new Tuple<List<T1>, List<T2>, List<T3>>(t1, t2, t3);
        }

        public async Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>>>> ToTupleOp<T1, T2, T3>(string script, object? parameters, CancellationToken cancellationToken)
        {
            var response = new DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>>>();

            try
            {
                response.Result = await ToTupleAsync<T1, T2, T3>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || DatabaseProvider.IsCancellatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>>>>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(in script, in ex, in parameters);
                response = DbOpResult.ErrorResult<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>>>>(msg);
            }

            return response;
        }

        public async Task<Tuple<List<T1>, List<T2>, List<T3>>> ToTupleAsync<T1, T2, T3>(string script, object? parameters, CancellationToken cancellationToken)
        {
            await using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryTuple3Config, in _buffered, in _transaction, in _command);
            await command.PrepareAsync(cancellationToken);

            var t1 = await command.ReadListItemsAsync<T1>(cancellationToken);
            var t2 = await command.ReadListNextItemsAsync<T2>(cancellationToken);
            var t3 = await command.ReadListNextItemsAsync<T3>(cancellationToken);

            command.SetValuesOutFields();

            return new Tuple<List<T1>, List<T2>, List<T3>>(t1, t2, t3);
        }

        public async Task<Tuple<List<T1>, List<T2>, List<T3>>> ToTupleAsync<T1, T2, T3>(string script, object? parameters = null)
        {
            return await ToTupleAsync<T1, T2, T3>(script, parameters, CancellationToken.None);
        }

        private static readonly DbCommandConfiguration QueryTuple4Config = new DbCommandConfiguration(
                                                                           commandBehavior: CommandBehavior.Default,
                                                                           methodHandled: MethodHandled.ToTupleQueryString_4,
                                                                           keyAsReturnValue: false,
                                                                           generateParameterWithKeys: false);

        public DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> ToTupleOp<T1, T2, T3, T4>(in string script, in object? parameters)
        {
            var response = new DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>>();

            try
            {
                response.Result = ToTuple<T1, T2, T3, T4>(in script, in parameters);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(in script, in ex, in parameters);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>>>(in msg);
            }

            return response;
        }

        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>> ToTuple<T1, T2, T3, T4>(in string script, in object? parameters = null)
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

        public async Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>>> ToTupleOp<T1, T2, T3, T4>(string script, object? parameters, CancellationToken cancellationToken)
        {
            var response = new DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>>();

            try
            {
                response.Result = await ToTupleAsync<T1, T2, T3, T4>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || DatabaseProvider.IsCancellatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>>>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(in script, in ex, in parameters);
                response = DbOpResult.ErrorResult<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>>>(in msg);
            }

            return response;
        }

        public async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> ToTupleAsync<T1, T2, T3, T4>(string script, object? parameters, CancellationToken cancellationToken)
        {
            await using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryTuple4Config, in _buffered, in _transaction, in _command);
            await command.PrepareAsync(cancellationToken);

            var t1 = await command.ReadListItemsAsync<T1>(cancellationToken);
            var t2 = await command.ReadListNextItemsAsync<T2>(cancellationToken);
            var t3 = await command.ReadListNextItemsAsync<T3>(cancellationToken);
            var t4 = await command.ReadListNextItemsAsync<T4>(cancellationToken);

            command.SetValuesOutFields();

            return new Tuple<List<T1>, List<T2>, List<T3>, List<T4>>(t1, t2, t3, t4);
        }

        public async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> ToTupleAsync<T1, T2, T3, T4>(string script, object? parameters = null)
        {
            return await ToTupleAsync<T1, T2, T3, T4>(script, parameters, CancellationToken.None);
        }

        private static readonly DbCommandConfiguration QueryTuple5Config = new DbCommandConfiguration(
                                                                           commandBehavior: CommandBehavior.Default,
                                                                           methodHandled: MethodHandled.ToTupleQueryString_5,
                                                                           keyAsReturnValue: false,
                                                                           generateParameterWithKeys: false);

        public DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>> ToTupleOp<T1, T2, T3, T4, T5>(in string script, in object? parameters = null)
        {
            var response = new DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>>();

            try
            {
                response.Result = ToTuple<T1, T2, T3, T4, T5>(in script, in parameters);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(in script, in ex, in parameters);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>>>(in msg);
            }

            return response;
        }

        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>> ToTuple<T1, T2, T3, T4, T5>(in string script, in object? parameters = null)
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

        public async Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>>> ToTupleOp<T1, T2, T3, T4, T5>(string script, object? parameters, CancellationToken cancellationToken)
        {
            var response = new DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>>();

            try
            {
                response.Result = await ToTupleAsync<T1, T2, T3, T4, T5>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || DatabaseProvider.IsCancellatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>>>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(in script, in ex, in parameters);
                response = DbOpResult.ErrorResult<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>>>(msg);
            }

            return response;
        }

        public async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>> ToTupleAsync<T1, T2, T3, T4, T5>(string script, object? parameters, CancellationToken cancellationToken)
        {
            await using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryTuple5Config, in _buffered, in _transaction, in _command);
            await command.PrepareAsync(cancellationToken);

            var t1 = await command.ReadListItemsAsync<T1>(cancellationToken);
            var t2 = await command.ReadListNextItemsAsync<T2>(cancellationToken);
            var t3 = await command.ReadListNextItemsAsync<T3>(cancellationToken);
            var t4 = await command.ReadListNextItemsAsync<T4>(cancellationToken);
            var t5 = await command.ReadListNextItemsAsync<T5>(cancellationToken);

            command.SetValuesOutFields();

            return new Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>(t1, t2, t3, t4, t5);
        }

        public async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>> ToTupleAsync<T1, T2, T3, T4, T5>(string script, object? parameters = null)
        {
            return await ToTupleAsync<T1, T2, T3, T4, T5>(script, parameters, CancellationToken.None);
        }

        private static readonly DbCommandConfiguration QueryTuple6Config = new DbCommandConfiguration(
                                                                           commandBehavior: CommandBehavior.Default,
                                                                           methodHandled: MethodHandled.ToTupleQueryString_6,
                                                                           keyAsReturnValue: false,
                                                                           generateParameterWithKeys: false);

        public DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>> ToTupleOp<T1, T2, T3, T4, T5, T6>(in string script, in object? parameters = null)
        {
            var response = new DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>>();

            try
            {
                response.Result = ToTuple<T1, T2, T3, T4, T5, T6>(in script, in parameters);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(in script, in ex, in parameters);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>>>(in msg);
            }

            return response;
        }

        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>> ToTuple<T1, T2, T3, T4, T5, T6>(in string script, in object? parameters = null)
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

        public async Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>>> ToTupleOp<T1, T2, T3, T4, T5, T6>(string script, object? parameters, CancellationToken cancellationToken)
        {
            var response = new DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>>();

            try
            {
                response.Result = await ToTupleAsync<T1, T2, T3, T4, T5, T6>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || DatabaseProvider.IsCancellatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>>>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(in script, in ex, in parameters);
                response = DbOpResult.ErrorResult<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>>>(msg);
            }

            return response;
        }

        public async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>> ToTupleAsync<T1, T2, T3, T4, T5, T6>(string script, object? parameters, CancellationToken cancellationToken)
        {
            await using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryTuple6Config, in _buffered, in _transaction, in _command);
            await command.PrepareAsync(cancellationToken);

            var t1 = await command.ReadListItemsAsync<T1>(cancellationToken);
            var t2 = await command.ReadListNextItemsAsync<T2>(cancellationToken);
            var t3 = await command.ReadListNextItemsAsync<T3>(cancellationToken);
            var t4 = await command.ReadListNextItemsAsync<T4>(cancellationToken);
            var t5 = await command.ReadListNextItemsAsync<T5>(cancellationToken);
            var t6 = await command.ReadListNextItemsAsync<T6>(cancellationToken);

            command.SetValuesOutFields();

            return new Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>(t1, t2, t3, t4, t5, t6);
        }

        public async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>> ToTupleAsync<T1, T2, T3, T4, T5, T6>(string script, object? parameters = null)
        {
            return await ToTupleAsync<T1, T2, T3, T4, T5, T6>(script, parameters, CancellationToken.None);
        }

        private static readonly DbCommandConfiguration QueryTuple7Config = new DbCommandConfiguration(
                                                                           commandBehavior: CommandBehavior.Default,
                                                                           methodHandled: MethodHandled.ToTupleQueryString_7,
                                                                           keyAsReturnValue: false,
                                                                           generateParameterWithKeys: false);

        public DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> ToTupleOp<T1, T2, T3, T4, T5, T6, T7>(in string script, in object? parameters = null)
        {
            var response = new DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>>();

            try
            {
                response.Result = ToTuple<T1, T2, T3, T4, T5, T6, T7>(in script, in parameters);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(in script, in ex, in parameters);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>>>(in msg);
            }

            return response;
        }

        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>> ToTuple<T1, T2, T3, T4, T5, T6, T7>(in string script, in object? parameters = null)
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

        public async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> ToTupleAsync<T1, T2, T3, T4, T5, T6, T7>(string script, object? parameters, CancellationToken cancellationToken)
        {
            await using var command = new DatabaseCommand(in Options, in script, in parameters, in QueryTuple7Config, in _buffered, in _transaction, in _command);
            await command.PrepareAsync(cancellationToken);

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

        public async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> ToTupleAsync<T1, T2, T3, T4, T5, T6, T7>(string script, object? parameters = null)
        {
            return await ToTupleAsync<T1, T2, T3, T4, T5, T6, T7>(script, parameters, CancellationToken.None);
        }

        public async Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>>> ToTupleOp<T1, T2, T3, T4, T5, T6, T7>(string script, object? parameters, CancellationToken cancellationToken)
        {
            var response = new DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>>();

            try
            {
                response.Result = await ToTupleAsync<T1, T2, T3, T4, T5, T6, T7>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || DatabaseProvider.IsCancellatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>>>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(in script, in ex, in parameters);
                response = DbOpResult.ErrorResult<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>>>(msg);
            }

            return response;
        }

        #endregion Multiple result set

        #region Error Handling

        private string ErrorDetailMessage(in string script, in Exception ex, in object? value = null)
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
                                                                   generateParameterWithKeys: false);

        public void Add<T>(T entity) where T : class, new()
        {
            var generator = new SqlGenerator<T>(in Formatter);
            var script = generator.GenerateInsert<T>(false);
            using var command = new DatabaseCommand(in Options, in script, entity, in AddConfig, in _buffered, in _transaction, in _command);
            command.Prepare();
            command.ExecuteNonQuery();
        }

        private static readonly DbCommandConfiguration AddReturnIDConfig = new DbCommandConfiguration(
                                                                           commandBehavior: CommandBehavior.Default,
                                                                           methodHandled: MethodHandled.Execute,
                                                                           keyAsReturnValue: true,
                                                                           generateParameterWithKeys: false);

        public TE Add<T, TE>(T entity) where T : class, new()
        {
            var generator = new SqlGenerator<T>(in Formatter);
            var script = generator.GenerateInsert<T>(true);
            using var command = new DatabaseCommand(in Options, in script, entity, in AddReturnIDConfig, in _buffered, in _transaction, in _command);
            command.Prepare();

            object? rawValue = null;
            if (Options.SqlProvider == SqlProvider.Oracle)
            {
                command.ExecuteNonQuery();
                var param = command.OutParameters.First();
                rawValue = DatabaseProvider.GetValueFromOracleParameter(param);
            }
            else
            {
                rawValue = command.ExecuteScalar();
            }

            return (TE)TypeConversionRegistry.ConvertByType(rawValue, typeof(TE), in Options.SqlProvider);
        }

        private static readonly DbCommandConfiguration UpdateConfig = new DbCommandConfiguration(
                                                                      commandBehavior: CommandBehavior.Default,
                                                                      methodHandled: MethodHandled.Execute,
                                                                      keyAsReturnValue: false,
                                                                      generateParameterWithKeys: false);

        public void Update<T>(T entity) where T : class, new()
        {
            var generator = new SqlGenerator<T>(in Formatter);
            var script = generator.GenerateUpdate();
            using var command = new DatabaseCommand(in Options, in script, entity, in UpdateConfig, in _buffered, in _transaction, in _command);
            command.Prepare();
            command.ExecuteNonQuery();
        }

        private static readonly DbCommandConfiguration DeleteConfig = new DbCommandConfiguration(
                                                                         commandBehavior: CommandBehavior.Default,
                                                                         methodHandled: MethodHandled.Execute,
                                                                         keyAsReturnValue: false,
                                                                         generateParameterWithKeys: true);

        public void Delete<T>(T entity) where T : class, new()
        {
            var generator = new SqlGenerator<T>(in Formatter);
            var script = generator.GenerateDelete();
            using var command = new DatabaseCommand(in Options, in script, entity, in DeleteConfig, in _buffered, in _transaction, in _command);
            command.Prepare();
            command.ExecuteNonQuery();
        }

        #endregion Write operations

        public static void Clear()
        {
            DynamicQueryString.Clear();
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