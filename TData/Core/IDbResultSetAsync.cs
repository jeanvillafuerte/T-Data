using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TData.Core
{
    public interface IDbResultSetAsync
    {
        /// <summary>
        /// Fetches data asynchronously in batches.
        /// </summary>
        /// <typeparam name="T">The type of the data to fetch.</typeparam>
        /// <param name="script">The SQL script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <param name="batchSize">The size of each batch.</param>
        /// <returns>A tuple containing an action and an asynchronous enumerable of lists of data.</returns>
        (Action, IAsyncEnumerable<List<T>>) FetchDataAsync<T>(string script, object parameters, int batchSize = 1000);

        /// <summary>
        /// Fetches data asynchronously in batches with cancellation token.
        /// </summary>
        /// <typeparam name="T">The type of the data to fetch.</typeparam>
        /// <param name="script">The SQL script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <param name="batchSize">The size of each batch.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A tuple containing an action and an asynchronous enumerable of lists of data.</returns>
        (Action, IAsyncEnumerable<List<T>>) FetchDataAsync<T>(string script, object parameters, int batchSize, CancellationToken cancellationToken);

        /// <summary>
        /// Executes a SQL script asynchronously.
        /// </summary>
        /// <param name="script">The SQL script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>The number of rows affected.</returns>
        Task<int> ExecuteAsync(string script, object parameters = null);

        /// <summary>
        /// Executes a SQL script asynchronously with a cancellation token.
        /// </summary>
        /// <param name="script">The SQL script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of rows affected.</returns>
        Task<int> ExecuteAsync(string script, object parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Executes a SQL script asynchronously and returns a scalar value.
        /// </summary>
        /// <param name="script">The SQL script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>The scalar value.</returns>
        Task<T> ExecuteScalarAsync<T>(string script, object parameters = null);

        /// <summary>
        /// Executes a SQL script asynchronously with a cancellation token and returns a scalar value.
        /// </summary>
        /// <param name="script">The SQL script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The scalar value.</returns>
        Task<T> ExecuteScalarAsync<T>(string script, object parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Fetches a single row asynchronously.
        /// </summary>
        /// <param name="script">The SQL script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>The fetched row.</returns>
        Task<T> FetchOneAsync<T>(string script, object parameters = null);

        /// <summary>
        /// Fetches a single row asynchronously with a cancellation token.
        /// </summary>
        /// <param name="script">The SQL script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The fetched row.</returns>
        Task<T> FetchOneAsync<T>(string script, object parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Fetches a list of rows asynchronously.
        /// </summary>
        /// <param name="script">The SQL script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>The list of fetched rows.</returns>
        Task<List<T>> FetchListAsync<T>(string script, object parameters = null);

        /// <summary>
        /// Fetches a list of rows asynchronously with a cancellation token.
        /// </summary>
        /// <param name="script">The SQL script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of fetched rows.</returns>
        Task<List<T>> FetchListAsync<T>(string script, object parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Fetches multiple lists of rows asynchronously.
        /// </summary>
        /// <param name="script">The SQL script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A tuple containing the lists of fetched rows.</returns>
        Task<Tuple<List<T1>, List<T2>>> FetchTupleAsync<T1, T2>(string script, object parameters = null);

        /// <summary>
        /// Fetches multiple lists of rows asynchronously.
        /// </summary>
        /// <param name="script">The SQL script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A tuple containing the lists of fetched rows.</returns>
        Task<Tuple<List<T1>, List<T2>, List<T3>>> FetchTupleAsync<T1, T2, T3>(string script, object parameters = null);

        /// <summary>
        /// Fetches multiple lists of rows asynchronously.
        /// </summary>
        /// <param name="script">The SQL script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A tuple containing the lists of fetched rows.</returns>
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> FetchTupleAsync<T1, T2, T3, T4>(string script, object parameters = null);

        /// <summary>
        /// Fetches multiple lists of rows asynchronously.
        /// </summary>
        /// <param name="script">The SQL script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A tuple containing the lists of fetched rows.</returns>
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>> FetchTupleAsync<T1, T2, T3, T4, T5>(string script, object parameters = null);

        /// <summary>
        /// Fetches multiple lists of rows asynchronously.
        /// </summary>
        /// <param name="script">The SQL script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A tuple containing the lists of fetched rows.</returns>
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>> FetchTupleAsync<T1, T2, T3, T4, T5, T6>(string script, object parameters = null);

        /// <summary>
        /// Fetches multiple lists of rows asynchronously.
        /// </summary>
        /// <param name="script">The SQL script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <returns>A tuple containing the lists of fetched rows.</returns>
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> FetchTupleAsync<T1, T2, T3, T4, T5, T6, T7>(string script, object parameters = null);

        /// <summary>
        /// Fetches multiple lists of rows asynchronously with a cancellation token.
        /// </summary>
        /// <param name="script">The SQL script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A tuple containing the lists of fetched rows.</returns>
        Task<Tuple<List<T1>, List<T2>>> FetchTupleAsync<T1, T2>(string script, object parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Fetches multiple lists of rows asynchronously with a cancellation token.
        /// </summary>
        /// <param name="script">The SQL script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A tuple containing the lists of fetched rows.</returns>
        Task<Tuple<List<T1>, List<T2>, List<T3>>> FetchTupleAsync<T1, T2, T3>(string script, object parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Fetches multiple lists of rows asynchronously with a cancellation token.
        /// </summary>
        /// <param name="script">The SQL script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A tuple containing the lists of fetched rows.</returns>
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> FetchTupleAsync<T1, T2, T3, T4>(string script, object parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Fetches multiple lists of rows asynchronously with a cancellation token.
        /// </summary>
        /// <param name="script">The SQL script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A tuple containing the lists of fetched rows.</returns>
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>>> FetchTupleAsync<T1, T2, T3, T4, T5>(string script, object parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Fetches multiple lists of rows asynchronously with a cancellation token.
        /// </summary>
        /// <param name="script">The SQL script to execute.</param>
        /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A tuple containing the lists of fetched rows.</returns>
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>>> FetchTupleAsync<T1, T2, T3, T4, T5, T6>(string script, object parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Fetches multiple lists of rows asynchronously with a cancellation token.
        /// </summary>
        /// <param name="script">The SQL script to execute.</param>
                /// <param name="parameters">
        /// The parameters for the script. Can be an anonymous object or a defined class.
        /// If using a defined class, consider applying the <see cref="TData.Attributes.DbParameterAttribute"/> to manage additional parameter info.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A tuple containing the lists of fetched rows.</returns>
        Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> FetchTupleAsync<T1, T2, T3, T4, T5, T6, T7>(string script, object parameters, CancellationToken cancellationToken);
    }
}
