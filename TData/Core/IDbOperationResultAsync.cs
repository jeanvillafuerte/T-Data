using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TData.Core
{
    public interface IDbOperationResultAsync
    {
        /// <summary>
        /// Tries to execute a script asynchronously.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A task representing the asynchronous operation result</returns>
        Task<DbOpAsyncResult> TryExecuteAsync(string script, object parameters = null);

        /// <summary>
        /// Tries to execute a script asynchronously with a cancellation token.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation result</returns>
        Task<DbOpAsyncResult> TryExecuteAsync(string script, object parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Tries to execute a scalar script asynchronously.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A task representing the asynchronous operation result</returns>
        Task<DbOpAsyncResult<T>> TryExecuteScalarAsync<T>(string script, object parameters = null);

        /// <summary>
        /// Tries to execute a scalar script asynchronously with a cancellation token.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation result</returns>
        Task<DbOpAsyncResult<T>> TryExecuteScalarAsync<T>(string script, object parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Tries to fetch one result asynchronously.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A task representing the asynchronous operation result</returns>
        Task<DbOpAsyncResult<T>> TryFetchOneAsync<T>(string script, object parameters = null);

        /// <summary>
        /// Tries to fetch one result asynchronously with a cancellation token.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation result</returns>
        Task<DbOpAsyncResult<T>> TryFetchOneAsync<T>(string script, object parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Tries to fetch a list of results asynchronously.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A task representing the asynchronous operation result</returns>
        Task<DbOpAsyncResult<List<T>>> TryFetchListAsync<T>(string script, object parameters = null);

        /// <summary>
        /// Tries to fetch a list of results asynchronously with a cancellation token.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation result</returns>
        Task<DbOpAsyncResult<List<T>>> TryFetchListAsync<T>(string script, object parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Tries to fetch a tuple of two lists of results asynchronously.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A task representing the asynchronous operation result</returns>
        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>>>> TryFetchTupleAsync<T1, T2>(string script, object parameters = null);

        /// <summary>
        /// Tries to fetch a tuple of two lists of results asynchronously with a cancellation token.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation result</returns>
        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>>>> TryFetchTupleAsync<T1, T2>(string script, object parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Tries to fetch a tuple of three lists of results asynchronously.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A task representing the asynchronous operation result</returns>
        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>>>> TryFetchTupleAsync<T1, T2, T3>(string script, object parameters = null);

        /// <summary>
        /// Tries to fetch a tuple of three lists of results asynchronously with a cancellation token.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation result</returns>
        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>>>> TryFetchTupleAsync<T1, T2, T3>(string script, object parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Tries to fetch a tuple of four lists of results asynchronously.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A task representing the asynchronous operation result</returns>
        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>>> TryFetchTupleAsync<T1, T2, T3, T4>(string script, object parameters = null);

        /// <summary>
        /// Tries to fetch a tuple of four lists of results asynchronously with a cancellation token.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation result</returns>
        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>>> TryFetchTupleAsync<T1, T2, T3, T4>(string script, object parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Tries to fetch a tuple of five lists of results asynchronously.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A task representing the asynchronous operation result</returns>
        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>>> TryFetchTupleAsync<T1, T2, T3, T4, T5>(string script, object parameters = null);

        /// <summary>
        /// Tries to fetch a tuple of five lists of results asynchronously with a cancellation token.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation result</returns>
        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>>> TryFetchTupleAsync<T1, T2, T3, T4, T5>(string script, object parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Tries to fetch a tuple of six lists of results asynchronously.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A task representing the asynchronous operation result</returns>
        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>>> TryFetchTupleAsync<T1, T2, T3, T4, T5, T6>(string script, object parameters = null);

        /// <summary>
        /// Tries to fetch a tuple of six lists of results asynchronously with a cancellation token.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation result</returns>
        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>>> TryFetchTupleAsync<T1, T2, T3, T4, T5, T6>(string script, object parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Tries to fetch a tuple of seven lists of results asynchronously.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A task representing the asynchronous operation result</returns>
        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>>> TryFetchTupleAsync<T1, T2, T3, T4, T5, T6, T7>(string script, object parameters = null);

        /// <summary>
        /// Tries to fetch a tuple of seven lists of results asynchronously with a cancellation token.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation result</returns>
        Task<DbOpAsyncResult<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>>> TryFetchTupleAsync<T1, T2, T3, T4, T5, T6, T7>(string script, object parameters, CancellationToken cancellationToken);
    }
}
