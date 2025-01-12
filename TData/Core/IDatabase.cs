using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using TData.Core;
using TData.Core.QueryGenerator;
using TData.Core.WriteDatabase;

namespace TData
{
    /// <summary>
    /// Interface representing a database with various operations.
    /// </summary>
    public interface IDatabase : IWriteOnlyDatabase, IDbOperationResult, IDbResultSet, IDbOperationResultAsync, IDbResultSetAsync
    {
        /// <summary>
        /// Executes a script.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>The number of affected rows.</returns>
        int Execute(in string script, in object parameters = null);

        /// <summary>
        /// Executes a script and returns a scalar value.
        /// </summary>
        /// <typeparam name="T">The type of the scalar value.</typeparam>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>The scalar value.</returns>
        T ExecuteScalar<T>(in string script, in object parameters = null);

        void LoadStream(in string script, in object parameters, in Stream targetStream);

        Task LoadStreamAsync(string script, object parameters, Stream targetStream);

        Task LoadStreamAsync(string script, object parameters, Stream targetStream, CancellationToken cancellationToken);

        void LoadTextStream(in string script, in object parameters, in StreamWriter targetStream);

        Task LoadTextStreamAsync(string script, object parameters, StreamWriter targetStream);

        Task LoadTextStreamAsync(string script, object parameters, StreamWriter targetStream, CancellationToken cancellationToken);

        /// <summary>
        /// Fetches a single record.
        /// </summary>
        /// <typeparam name="T">The type of the record.</typeparam>
        /// <param name="where">The where clause expression.</param>
        /// <param name="selector">The selector expression.</param>
        /// <returns>The fetched record.</returns>
        T FetchOne<T>(Expression<Func<T, bool>> where = null, Expression<Func<T, object>> selector = null);

        /// <summary>
        /// Fetches a list of records.
        /// </summary>
        /// <typeparam name="T">The type of the records.</typeparam>
        /// <param name="where">The where clause expression.</param>
        /// <param name="selector">The selector expression.</param>
        /// <returns>The list of fetched records.</returns>
        List<T> FetchList<T>(Expression<Func<T, bool>> where = null, Expression<Func<T, object>> selector = null);

        /// <summary>
        /// Fetches data in batches.
        /// </summary>
        /// <typeparam name="T">The type of the data.</typeparam>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <param name="batchSize">The size of each batch.</param>
        /// <returns>A tuple containing an action and an enumerable of lists of data.</returns>
        (Action, IEnumerable<List<T>>) FetchData<T>(string script, object parameters = null, int batchSize = 1000);

        /// <summary>
        /// Executes a block of code within the context of the database.
        /// Internally perform db request using the same command instance while keep the connection open.
        /// </summary>
        /// <param name="func">The block of code to execute.</param>
        void ExecuteBlock(Action<IDatabase> func);

        /// <summary>
        /// Asynchronously fetches a single record.
        /// </summary>
        /// <typeparam name="T">The type of the record.</typeparam>
        /// <param name="where">The where clause expression.</param>
        /// <param name="selector">The selector expression.</param>
        /// <returns>A task representing the asynchronous operation, with the fetched record as the result.</returns>
        Task<T> FetchOneAsync<T>(Expression<Func<T, bool>> where = null, Expression<Func<T, object>> selector = null);

        /// <summary>
        /// Asynchronously fetches a single records with cancellation support.
        /// </summary>
        /// <typeparam name="T">The type of the record.</typeparam>
        /// <param name="where">The where clause expression.</param>
        /// <param name="selector">The selector expression.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, with the fetched record as the result.</returns>
        Task<T> FetchOneAsync<T>(Expression<Func<T, bool>> where, Expression<Func<T, object>> selector, CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously fetches a list of records.
        /// </summary>
        /// <typeparam name="T">The type of the records.</typeparam>
        /// <param name="where">The where clause expression.</param>
        /// <param name="selector">The selector expression.</param>
        /// <returns>A task representing the asynchronous operation, with the list of fetched records as the result.</returns>
        Task<List<T>> FetchListAsync<T>(Expression<Func<T, bool>> where = null, Expression<Func<T, object>> selector = null);

        /// <summary>
        /// Asynchronously fetches a list of records with cancellation support.
        /// </summary>
        /// <typeparam name="T">The type of the records.</typeparam>
        /// <param name="where">The where clause expression.</param>
        /// <param name="selector">The selector expression.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, with the list of fetched records as the result.</returns>
        Task<List<T>> FetchListAsync<T>(Expression<Func<T, bool>> where, Expression<Func<T, object>> selector, CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously executes a block of code within the context of the database.
        /// </summary>
        /// <param name="func">The block of code to execute.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ExecuteBlockAsync(Func<IDatabase, Task> func);

        /// <summary>
        /// Asynchronously executes a block of code within the context of the database, with cancellation support.
        /// </summary>
        /// <param name="func">The block of code to execute.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ExecuteBlockAsync(Func<IDatabase, CancellationToken, Task> func, CancellationToken cancellationToken);

        /// <summary>
        /// Executes a transaction.
        /// </summary>
        /// <param name="func">The function to execute within the transaction.</param>
        /// <returns>True if the transaction was successful, otherwise false.</returns>
        bool ExecuteTransaction(Func<IDatabase, TransactionResult> func);

        /// <summary>
        /// Executes a transaction and returns a result.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="func">The function to execute within the transaction.</param>
        /// <returns>The result of the transaction.</returns>
        T ExecuteTransaction<T>(Func<IDatabase, T> func);

        /// <summary>
        /// Commits the current transaction.
        /// </summary>
        /// <returns>The result of the commit operation.</returns>
        TransactionResult Commit();

        /// <summary>
        /// Rolls back the current transaction.
        /// </summary>
        /// <returns>The result of the rollback operation.</returns>
        TransactionResult Rollback();

        /// <summary>
        /// Asynchronously executes a transaction with the given function and returns a result.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="func">The function to execute within the transaction.</param>
        /// <returns>A task representing the asynchronous operation, with the result of the transaction as the result.</returns>
        Task<T> ExecuteTransactionAsync<T>(Func<IDatabase, Task<T>> func);

        /// <summary>
        /// Asynchronously executes a transaction with the given function.
        /// </summary>
        /// <param name="func">The function to execute within the transaction.</param>
        /// <returns>A task representing the asynchronous operation, with a boolean indicating the success of the transaction as the result.</returns>
        Task<bool> ExecuteTransaction(Func<IDatabase, Task<TransactionResult>> func);

        /// <summary>
        /// Asynchronously executes a transaction with the given function and returns a result, with cancellation support.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="func">The function to execute within the transaction.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, with the result of the transaction as the result.</returns>
        Task<T> ExecuteTransactionAsync<T>(Func<IDatabase, CancellationToken, Task<T>> func, CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously executes a transaction with the given function, with cancellation support.
        /// </summary>
        /// <param name="func">The function to execute within the transaction.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, with a boolean indicating the success of the transaction as the result.</returns>
        Task<bool> ExecuteTransaction(Func<IDatabase, CancellationToken, Task<TransactionResult>> func, CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously commits the current transaction.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, with the result of the commit operation as the result.</returns>
        Task<TransactionResult> CommitAsync();

        /// <summary>
        /// Asynchronously commits the current transaction, with cancellation support.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, with the result of the commit operation as the result.</returns>
        Task<TransactionResult> CommitAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously rolls back the current transaction.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, with the result of the rollback operation as the result.</returns>
        Task<TransactionResult> RollbackAsync();
    }
}
