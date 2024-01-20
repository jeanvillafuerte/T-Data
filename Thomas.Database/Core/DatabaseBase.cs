using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Thomas.Cache")]
[assembly: InternalsVisibleTo("Thomas.Database.Tests")]

namespace Thomas.Database
{
    using System.Data.Common;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Thomas.Database.Cache.Metadata;

    public sealed class DatabaseBase : IDatabase
    {
        private readonly IDatabaseProvider Provider;
        public readonly DbSettings Options;

        private DbTransaction _transaction;
        private DbCommand _command;
        private bool _transactionCompleted;

        internal DatabaseBase(in IDatabaseProvider provider, in DbSettings options)
        {
            Provider = provider;
            Options = options;
        }

        #region transaction
        public T ExecuteTransaction<T>(Func<IDatabase, T> func)
        {
            using var command = new DatabaseCommand(in Provider, in Options);
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
            }
        }

        public async Task<T> ExecuteTransactionAsync<T>(Func<IDatabase, T> func, CancellationToken cancellationToken)
        {
            using var command = new DatabaseCommand(in Provider, in Options);
            _transaction = command.BeginTransaction();
            _command = command.CreateEmptyCommand();

            try
            {
                var result = await Task.Run(() => func(this), cancellationToken);
                await _transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch (Exception)
            {
                if (!_transactionCompleted)
                {
                    await _transaction.RollbackAsync(cancellationToken);
                    throw;
                }

                return default;
            }
            finally
            {
                _transaction.Dispose();
            }
        }

        public bool ExecuteTransaction(Func<IDatabase, TransactionResult> func)
        {
            using var command = new DatabaseCommand(in Provider, in Options);
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
            }
        }

        public async Task<bool> ExecuteTransaction(Func<IDatabase, TransactionResult> func, CancellationToken cancellationToken)
        {
            using var command = new DatabaseCommand(in Provider, in Options);
            _transaction = command.BeginTransaction();
            _command = command.CreateEmptyCommand();

            try
            {
                var result = await Task.Run(() => func(this), cancellationToken);
                await _transaction.CommitAsync(cancellationToken);
                return result == TransactionResult.Committed;
            }
            catch (Exception)
            {
                if (!_transactionCompleted)
                {
                    await _transaction.RollbackAsync(cancellationToken);
                    throw;
                }

                return false;
            }
            finally
            {
                _transaction.Dispose();
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

        public async Task<TransactionResult> CommitAsync()
        {
            if (_transaction == null)
                throw new InvalidOperationException("Transaction not started");

            _transactionCompleted = true;
            await _transaction.CommitAsync();
            return TransactionResult.Committed;
        }

        public async Task<TransactionResult> RollbackAsync()
        {
            if (_transaction == null)
                throw new InvalidOperationException("Transaction not started");

            _transactionCompleted = true;
            await _transaction.RollbackAsync();
            return TransactionResult.Rollbacked;
        }

        #endregion

        #region without result data

        public int Execute(string script, object? parameters = null)
        {
            using var command = new DatabaseCommand(in Provider, in Options, in script, in parameters, in _transaction, in _command);

            command.Prepare();
            var affected = command.ExecuteNonQuery();
            if (command.OutParameters != null)
                command.SetValuesOutFields();
            return affected;
        }

        public DbOpResult ExecuteOp(string script, object? parameters = null)
        {
            var response = new DbOpResult() { Success = true };

            try
            {
                response.RowsAffected = Execute(script, parameters);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(in script, in ex);
                response = DbOpResult.ErrorResult<DbOpResult>(in msg);
            }

            return response;
        }

        public async Task<DbOpAsyncResult> ExecuteOpAsync(string script, object? parameters, CancellationToken cancellationToken)
        {
            var response = new DbOpAsyncResult() { Success = true };
            try
            {
                using var command = new DatabaseCommand(Provider, Options, in script, in parameters);
                await command.PrepareAsync(cancellationToken);
                response.RowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

                command.SetValuesOutFields();
            }
            catch (Exception ex) when (ex is TaskCanceledException || Provider.IsCancellatedOperationException(in ex))
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
            using var command = new DatabaseCommand(Provider, Options, in script, in parameters);
            await command.PrepareAsync(cancellationToken);
            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            command.SetValuesOutFields();
            return affected;
        }

        #endregion

        #region single row result

        public T? ToSingle<T>(string script, object? parameters = null) where T : class, new()
        {
            using var command = new DatabaseCommand(Provider, Options, in script, in parameters);
            command.Prepare();
            var item = command.ReadListItems<T>(CommandBehavior.SingleRow).FirstOrDefault();
            command.SetValuesOutFields();
            return item;
        }

        public DbOpResult<T> ToSingleOp<T>(string script, object? parameters = null) where T : class, new()
        {
            var response = new DbOpResult<T>() { Success = true };

            try
            {
                response.Result = ToSingle<T>(script, parameters);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(in script, in ex, in parameters);
                response = DbOpResult.ErrorResult<DbOpResult<T>>(in msg);
            }

            return response;
        }

        public async Task<T?> ToSingleAsync<T>(string script, object? parameters, CancellationToken cancellationToken) where T : class, new()
        {
            using var command = new DatabaseCommand(Provider, Options, in script, in parameters);
            await command.PrepareAsync(cancellationToken);
            return command.ReadListItems<T>(CommandBehavior.SingleRow).FirstOrDefault();
        }

        public async Task<DbOpAsyncResult<T>> ToSingleOpAsync<T>(string script, object? parameters, CancellationToken cancellationToken) where T : class, new()
        {
            var response = new DbOpAsyncResult<T>() { Success = true };

            try
            {
                response.Result = await ToSingleAsync<T>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || Provider.IsCancellatedOperationException(in ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult<T>>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(in script, in ex);
                response = DbOpResult.ErrorResult<DbOpAsyncResult<T>>(in msg);
            }

            return response;
        }

        #endregion

        #region one result set

        public DbOpResult<IEnumerable<T>> ToListOp<T>(string script, object? parameters = null) where T : class, new()
        {
            var response = new DbOpResult<IEnumerable<T>>() { Success = true };

            try
            {
                response.Result = ToList<T>(script, parameters);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(in script, in ex, in parameters);
                response = DbOpResult.ErrorResult<DbOpResult<IEnumerable<T>>>(in msg);
            }

            return response;
        }

        public IEnumerable<T> ToList<T>(string script, object? parameters = null) where T : class, new()
        {
            using var command = new DatabaseCommand(Provider, Options, in script, in parameters, in _transaction, in _command);
            command.Prepare();

            var result = command.ReadListItems<T>(CommandBehavior.SingleResult);

            command.SetValuesOutFields();

            return result;
        }

        public async Task<IEnumerable<T>> ToListAsync<T>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken) where T : class, new()
        {
            using var command = new DatabaseCommand(Provider, Options, in script, parameters);

            await command.PrepareAsync(cancellationToken);

            var result = await command.ReadListItemsAsync<T>(CommandBehavior.SingleResult, cancellationToken);

            command.SetValuesOutFields();

            return result;
        }

        public async Task<DbOpAsyncResult<IEnumerable<T>>> ToListOpAsync<T>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken) where T : class, new()
        {
            var response = new DbOpAsyncResult<IEnumerable<T>>() { Success = true };

            try
            {
                response.Result = await ToListAsync<T>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || Provider.IsCancellatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult<IEnumerable<T>>>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpAsyncResult<IEnumerable<T>>>(msg);
            }

            return response;
        }

        #endregion

        #region Multiple result set

        public DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>> ToTupleOp<T1, T2>(string script, object? parameters = null)
           where T1 : class, new()
           where T2 : class, new()
        {
            var response = new DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>>();

            try
            {
                response.Result = ToTuple<T1, T2>(script, parameters);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>>>(msg);
            }

            return response;
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>> ToTuple<T1, T2>(string script, object? parameters = null) where T1 : class, new() where T2 : class, new()
        {
            using var command = new DatabaseCommand(Provider, Options, in script, in parameters);
            command.Prepare();
            var t1 = command.ReadListItems<T1>(CommandBehavior.Default);
            var t2 = command.ReadListNextItems<T2>();

            command.SetValuesOutFields();

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>>(t1.ToList(), t2.ToList());
        }

        public async Task<Tuple<IEnumerable<T1>, IEnumerable<T2>>> ToTupleAsync<T1, T2>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken) where T1 : class, new() where T2 : class, new()
        {
            var command = new DatabaseCommand(Provider, Options, in script, in parameters);
            await command.PrepareAsync(cancellationToken);

            var t1 = await command.ReadListItemsAsync<T1>(CommandBehavior.Default, cancellationToken);
            var t2 = await command.ReadListNextItemsAsync<T2>(cancellationToken);

            command.SetValuesOutFields();

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>>(t1, t2);
        }

        public async Task<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>>> ToTupleOpAsync<T1, T2>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
           where T1 : class, new()
           where T2 : class, new()
        {
            var response = new DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>>();

            try
            {
                response.Result = await ToTupleAsync<T1, T2>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || Provider.IsCancellatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>>>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>>>>(msg);
            }

            return response;
        }

        public DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>> ToTupleOp<T1, T2, T3>(string script, object? parameters = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
        {
            var response = new DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>();

            try
            {
                response.Result = ToTuple<T1, T2, T3>(script, parameters);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>>(msg);
            }

            return response;
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>> ToTuple<T1, T2, T3>(string script, object? parameters = null) where T1 : class, new() where T2 : class, new() where T3 : class, new()
        {
            using var command = new DatabaseCommand(Provider, Options, in script, in parameters);
            command.Prepare();

            var t1 = command.ReadListItems<T1>(CommandBehavior.Default);
            var t2 = command.ReadListNextItems<T2>();
            var t3 = command.ReadListNextItems<T3>();

            command.SetValuesOutFields();

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>(t1, t2, t3);
        }

        public async Task<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>> ToTupleOp<T1, T2, T3>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
        {
            var response = new DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>();

            try
            {
                response.Result = await ToTupleAsync<T1, T2, T3>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || Provider.IsCancellatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>>(msg);
            }

            return response;
        }

        public async Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>> ToTupleAsync<T1, T2, T3>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken) where T1 : class, new() where T2 : class, new() where T3 : class, new()
        {
            using var command = new DatabaseCommand(Provider, Options, in script, in parameters);
            await command.PrepareAsync(cancellationToken);

            var t1 = await command.ReadListItemsAsync<T1>(CommandBehavior.Default, cancellationToken);
            var t2 = await command.ReadListNextItemsAsync<T2>(cancellationToken);
            var t3 = await command.ReadListNextItemsAsync<T3>(cancellationToken);

            command.SetValuesOutFields();

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>(t1, t2, t3);
        }

        public DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>> ToTupleOp<T1, T2, T3, T4>(string script, object? parameters)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
        {
            var response = new DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>();

            try
            {
                response.Result = ToTuple<T1, T2, T3, T4>(script, parameters);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>>(msg);
            }

            return response;
        }

        public async Task<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>> ToTupleOp<T1, T2, T3, T4>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
        {
            var response = new DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>();

            try
            {
                response.Result = await ToTupleAsync<T1, T2, T3, T4>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || Provider.IsCancellatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>>(msg);
            }

            return response;
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>> ToTuple<T1, T2, T3, T4>(string script, object? parameters = null) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new()
        {
            using var command = new DatabaseCommand(Provider, Options, in script, in parameters);
            command.Prepare();

            var t1 = command.ReadListItems<T1>(CommandBehavior.Default);
            var t2 = command.ReadListNextItems<T2>();
            var t3 = command.ReadListNextItems<T3>();
            var t4 = command.ReadListNextItems<T4>();

            command.SetValuesOutFields();

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>(t1, t2, t3, t4);
        }

        public async Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>> ToTupleAsync<T1, T2, T3, T4>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new()
        {
            using var command = new DatabaseCommand(Provider, Options, in script, in parameters);
            await command.PrepareAsync(cancellationToken);

            var t1 = await command.ReadListItemsAsync<T1>(CommandBehavior.Default, cancellationToken);
            var t2 = await command.ReadListNextItemsAsync<T2>(cancellationToken);
            var t3 = await command.ReadListNextItemsAsync<T3>(cancellationToken);
            var t4 = await command.ReadListNextItemsAsync<T4>(cancellationToken);

            command.SetValuesOutFields();

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>(t1, t2, t3, t4);
        }

        public async Task<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>> ToTupleOp<T1, T2, T3, T4, T5>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
        {
            var response = new DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>();

            try
            {
                response.Result = await ToTupleAsync<T1, T2, T3, T4, T5>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || Provider.IsCancellatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>>(msg);
            }

            return response;
        }

        public DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>> ToTupleOp<T1, T2, T3, T4, T5>(string script, object? parameters = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
        {
            var response = new DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>();

            try
            {
                response.Result = ToTuple<T1, T2, T3, T4, T5>(script, parameters);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>>(msg);
            }

            return response;
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>> ToTuple<T1, T2, T3, T4, T5>(string script, object? parameters = null) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new()
        {
            using var command = new DatabaseCommand(Provider, Options, in script, in parameters);
            command.Prepare();

            var t1 = command.ReadListItems<T1>(CommandBehavior.Default);
            var t2 = command.ReadListNextItems<T2>();
            var t3 = command.ReadListNextItems<T3>();
            var t4 = command.ReadListNextItems<T4>();
            var t5 = command.ReadListNextItems<T5>();

            command.SetValuesOutFields();

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>(t1, t2, t3, t4, t5);
        }

        public async Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>> ToTupleAsync<T1, T2, T3, T4, T5>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new()
        {
            using var command = new DatabaseCommand(Provider, Options, in script, in parameters);
            await command.PrepareAsync(cancellationToken);

            var t1 = await command.ReadListItemsAsync<T1>(CommandBehavior.Default, cancellationToken);
            var t2 = await command.ReadListNextItemsAsync<T2>(cancellationToken);
            var t3 = await command.ReadListNextItemsAsync<T3>(cancellationToken);
            var t4 = await command.ReadListNextItemsAsync<T4>(cancellationToken);
            var t5 = await command.ReadListNextItemsAsync<T5>(cancellationToken);

            command.SetValuesOutFields();

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>(t1, t2, t3, t4, t5);
        }

        public async Task<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>> ToTupleOp<T1, T2, T3, T4, T5, T6>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
        {
            var response = new DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>();

            try
            {
                response.Result = await ToTupleAsync<T1, T2, T3, T4, T5, T6>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || Provider.IsCancellatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>>(msg);
            }

            return response;
        }

        public DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>> ToTupleOp<T1, T2, T3, T4, T5, T6>(string script, object? parameters = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
        {
            var response = new DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>();

            try
            {
                response.Result = ToTuple<T1, T2, T3, T4, T5, T6>(script, parameters);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>>(msg);
            }

            return response;
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>> ToTuple<T1, T2, T3, T4, T5, T6>(string script, object? parameters = null) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new()
        {
            using var command = new DatabaseCommand(Provider, Options, in script, in parameters);
            command.Prepare();

            var t1 = command.ReadListItems<T1>(CommandBehavior.Default);
            var t2 = command.ReadListNextItems<T2>();
            var t3 = command.ReadListNextItems<T3>();
            var t4 = command.ReadListNextItems<T4>();
            var t5 = command.ReadListNextItems<T5>();
            var t6 = command.ReadListNextItems<T6>();

            command.SetValuesOutFields();

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>(t1, t2, t3, t4, t5, t6);
        }

        public async Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>> ToTupleAsync<T1, T2, T3, T4, T5, T6>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new()
        {
            using var command = new DatabaseCommand(Provider, Options, in script, in parameters);
            await command.PrepareAsync(cancellationToken);

            var t1 = await command.ReadListItemsAsync<T1>(CommandBehavior.Default, cancellationToken);
            var t2 = await command.ReadListNextItemsAsync<T2>(cancellationToken);
            var t3 = await command.ReadListNextItemsAsync<T3>(cancellationToken);
            var t4 = await command.ReadListNextItemsAsync<T4>(cancellationToken);
            var t5 = await command.ReadListNextItemsAsync<T5>(cancellationToken);
            var t6 = await command.ReadListNextItemsAsync<T6>(cancellationToken);

            command.SetValuesOutFields();

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>(t1, t2, t3, t4, t5, t6);
        }

        public DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>> ToTupleOp<T1, T2, T3, T4, T5, T6, T7>(string script, object? parameters = null)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
            where T4 : class, new()
            where T5 : class, new()
            where T6 : class, new()
            where T7 : class, new()
        {
            var response = new DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>();

            try
            {
                response.Result = ToTuple<T1, T2, T3, T4, T5, T6, T7>(script, parameters);
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>>(msg);
            }

            return response;
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>> ToTuple<T1, T2, T3, T4, T5, T6, T7>(string script, object? parameters = null) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new() where T7 : class, new()
        {
            using var command = new DatabaseCommand(Provider, Options, in script, in parameters);
            command.Prepare();

            var t1 = command.ReadListItems<T1>(CommandBehavior.Default);
            var t2 = command.ReadListNextItems<T2>();
            var t3 = command.ReadListNextItems<T3>();
            var t4 = command.ReadListNextItems<T4>();
            var t5 = command.ReadListNextItems<T5>();
            var t6 = command.ReadListNextItems<T6>();
            var t7 = command.ReadListNextItems<T7>();

            command.SetValuesOutFields();

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>(t1, t2, t3, t4, t5, t6, t7);
        }

        public async Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>> ToTupleAsync<T1, T2, T3, T4, T5, T6, T7>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new() where T7 : class, new()
        {
            using var command = new DatabaseCommand(Provider, Options, in script, in parameters);
            await command.PrepareAsync(cancellationToken);

            var t1 = await command.ReadListItemsAsync<T1>(CommandBehavior.Default, cancellationToken);
            var t2 = await command.ReadListNextItemsAsync<T2>(cancellationToken);
            var t3 = await command.ReadListNextItemsAsync<T3>(cancellationToken);
            var t4 = await command.ReadListNextItemsAsync<T4>(cancellationToken);
            var t5 = await command.ReadListNextItemsAsync<T5>(cancellationToken);
            var t6 = await command.ReadListNextItemsAsync<T6>(cancellationToken);
            var t7 = await command.ReadListNextItemsAsync<T7>(cancellationToken);

            command.SetValuesOutFields();

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>(t1, t2, t3, t4, t5, t6, t7);
        }

        public async Task<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>> ToTupleOp<T1, T2, T3, T4, T5, T6, T7>(string script, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
           where T1 : class, new()
           where T2 : class, new()
           where T3 : class, new()
           where T4 : class, new()
           where T5 : class, new()
           where T6 : class, new()
           where T7 : class, new()
        {
            var response = new DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>();

            try
            {
                response.Result = await ToTupleAsync<T1, T2, T3, T4, T5, T6, T7>(script, parameters, cancellationToken);
            }
            catch (Exception ex) when (ex is TaskCanceledException || Provider.IsCancellatedOperationException(ex))
            {
                response = DbOpAsyncResult.OperationCancelled<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>>();
            }
            catch (Exception ex)
            {
                var msg = ErrorDetailMessage(script, ex);
                response = DbOpResult.ErrorResult<DbOpAsyncResult<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>>(msg);
            }

            return response;
        }

        #endregion

        public IEnumerable<dynamic> GetMetadataParameter(string script, object? parameters)
        {
            return Provider.GetParams(HashHelper.GenerateHash($"Params_{script}", parameters));
        }

        #region Error Handling

        private string ErrorDetailMessage(in string script, in Exception excepcion, in object? value = null)
        {
            if (!Options.DetailErrorMessage)
            {
                return excepcion.Message;
            }

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Store Procedure/Script:");
            stringBuilder.AppendLine("\t" + script);

            if (value != null)
            {
                var hash = HashHelper.GenerateUniqueHash($"Inputs_{script}");

                MetadataPropertyInfo[] parameters = null;
                CacheResultInfo.TryGet(in hash, ref parameters);

                if (parameters != null && !Options.HideSensibleDataValue)
                {
                    stringBuilder.AppendLine("Parameters:");

                    foreach (var parameter in parameters)
                    {
                        stringBuilder.AppendLine(parameter.ErrorFormat(in value));
                    }
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

        #endregion
    }

    public enum TransactionResult
    {
        Committed,
        Rollbacked
    }
}